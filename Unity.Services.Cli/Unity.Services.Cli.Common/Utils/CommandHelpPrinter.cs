using System.CommandLine;
using System.CommandLine.Help;
using System.CommandLine.Invocation;

namespace Unity.Services.Cli;

public class CommandHelpPrinter
{
    readonly HelpBuilder m_HelpBuilder;
    readonly TextWriter m_Output;

    public CommandHelpPrinter(InvocationContext context, TextWriter output)
    {
        m_HelpBuilder = (HelpBuilder)context.BindingContext.GetService(typeof(HelpBuilder))!;
        m_Output = output;
    }

    public void PrintHelp(Command command, bool showHidden)
    {
        PrintHelpRecursive(command, showHidden, isFirst: true);
    }

    void PrintHelpRecursive(Command command, bool showHidden, bool isFirst)
    {
        if (!showHidden && command.IsHidden)
        {
            return;
        }

        if (!isFirst)
        {
            m_Output.WriteLine();
            m_Output.WriteLine(new string('=', 80));
            m_Output.WriteLine();
        }

        var commandPath = GetCommandPath(command);
        m_Output.WriteLine($"Command: {commandPath}");
        m_Output.WriteLine(new string('-', 80));

        if (showHidden)
        {
            command.IsHidden = false;
            foreach (var opt in command.Options) opt.IsHidden = false;
            foreach (var arg in command.Arguments) arg.IsHidden = false;
            foreach (var sub in command.Subcommands) sub.IsHidden = false;
        }

        var helpContext = new HelpContext(m_HelpBuilder, command, m_Output);
        m_HelpBuilder.Write(helpContext);

        foreach (var subcommand in command.Subcommands.OrderBy(c => c.Name))
        {
            PrintHelpRecursive(subcommand, showHidden, isFirst: false);
        }
    }

    static string GetCommandPath(Command command)
    {
        var parts = new List<string>();
        var current = command;
        while (current is not null)
        {
            parts.Insert(0, current.Name);
            current = current.Parents.OfType<Command>().FirstOrDefault();
        }

        return string.Join(" ", parts);
    }
}
