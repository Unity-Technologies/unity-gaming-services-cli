using System;
using System.CommandLine;
using System.CommandLine.Help;
using System.CommandLine.Invocation;
using System.Reflection;
using System.Text;

namespace Unity.Services.Cli;

public class CommandTreePrinter
{
    const int k_Indent = 2;

    readonly HelpBuilder m_HelpBuilder;
    readonly TextWriter m_Output;

    public CommandTreePrinter(InvocationContext context, TextWriter output)
    {
        m_HelpBuilder = (HelpBuilder)context.BindingContext.GetService(typeof(HelpBuilder))!;
        m_Output = output;
    }

    public void PrintUsage(Command command, bool showHidden)
    {
        PrintUsage(command, showHidden, 0);
    }

    void PrintUsage(Command command, bool showHidden, int level)
    {
        if (!showHidden && command.IsHidden)
        {
            return;
        }
        var usage = GetUsage(m_HelpBuilder, command, showHidden);
        m_Output.WriteLine(new string(Enumerable.Repeat(' ', k_Indent * level).ToArray()) + usage);
        level++;
        foreach (var subcommand in command.Children.Where(symbol => symbol is Command))
        {
            PrintUsage((subcommand as Command)!, showHidden, level);
        }
    }

    string GetUsage(HelpBuilder helpBuilder, Command command, bool showHidden)
    {
        return string.Join(" ", GetUsageParts().Where(x => !string.IsNullOrWhiteSpace(x)));

        IEnumerable<string> GetUsageParts()
        {
            List<Command> parentCommands = RecurseWhileNotNull(command, c => c.Parents.OfType<Command>().FirstOrDefault())
                    .Reverse()
                    .ToList();

            foreach (var parentCommand in parentCommands)
            {

                if (parentCommand == command)
                {
                    yield return string.Join("|", parentCommand.Aliases);
                }
                else
                {
                    yield return parentCommand.Name;
                }
                yield return FormatArgumentUsage(parentCommand.Arguments, showHidden);
            }

            var hasCommandWithHelp = command.Subcommands.Any(x => (showHidden || !x.IsHidden));

            if (hasCommandWithHelp)
            {
                yield return helpBuilder.LocalizationResources.HelpUsageCommand();
            }

            yield return FormatOptionsUsage(command, showHidden);

            if (!command.TreatUnmatchedTokensAsErrors)
            {
                yield return helpBuilder.LocalizationResources.HelpUsageAdditionalArguments();
            }
        }
    }

    static string FormatArgumentUsage(IReadOnlyList<Argument> arguments, bool showHidden)
    {
        var sb = new StringBuilder();

        var end = default(Stack<char>);

        for (var i = 0; i < arguments.Count; i++)
        {
            var argument = arguments[i];
            if (!showHidden && argument.IsHidden)
            {
                continue;
            }

            var arityIndicator =
                argument.Arity.MaximumNumberOfValues > 1
                    ? "..."
                    : "";

            var isOptional = IsOptional(argument);

            if (isOptional)
            {
                sb.Append($"[<{argument.Name}>{arityIndicator}");
                end ??= new Stack<char>();
                end.Push(']');
            }
            else
            {
                sb.Append($"<{argument.Name}>{arityIndicator}");
            }

            sb.Append(' ');
        }

        if (sb.Length > 0)
        {
            sb.Length--;

            if (end is { })
            {
                while (end.Count > 0)
                {
                    sb.Append(end.Pop());
                }
            }
        }

        return sb.ToString();

        bool IsOptional(Argument argument) => argument.Arity.MinimumNumberOfValues == 0;
    }

    string FormatOptionsUsage(Command command, bool showHidden)
    {
        var optionsUsage = new List<string>();
        HashSet<Option> uniqueOptions = new();

        List<Command> parentCommands = RecurseWhileNotNull(command, c => c.Parents.OfType<Command>().FirstOrDefault())
            .Reverse()
            .ToList();

        var globalOptions = parentCommands
            .SelectMany(cmd => cmd.Options)
            .Where(option => IsGlobal(option) && (showHidden || !option.IsHidden));

        var options = command.Options
            .Concat(globalOptions)
            .ToList();

        foreach (Option option in options)
        {
            if ((showHidden || !option.IsHidden) && uniqueOptions.Add(option))
            {
                var row = m_HelpBuilder.GetTwoColumnRow(option, new HelpContext(m_HelpBuilder, command, m_Output));
                var first = row.FirstColumnText;
                if (!option.IsRequired)
                {
                    first = $"[{first}]";
                }
                optionsUsage.Add(first);
            }
        }

        return string.Join(" ", optionsUsage.Where(o => !string.IsNullOrWhiteSpace(o)));
    }

    static IEnumerable<T> RecurseWhileNotNull<T>(T? source, Func<T, T?> next) where T : class
    {
        while (source is not null)
        {
            yield return source;

            source = next(source);
        }
    }

    PropertyInfo? m_IsGlobalProperty;

    // Option.IsGlobal is an internal property.
    bool IsGlobal(Option option)
    {
        return (bool)IsGlobalProperty()!.GetValue(option)!;
        PropertyInfo? IsGlobalProperty()
        {
            if (m_IsGlobalProperty == null)
            {
                m_IsGlobalProperty = typeof(Option).GetProperty("IsGlobal", BindingFlags.Instance | BindingFlags.NonPublic);
            }
            return m_IsGlobalProperty;
        }
    }
}
