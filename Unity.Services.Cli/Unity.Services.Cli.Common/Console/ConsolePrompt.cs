using Spectre.Console;

namespace Unity.Services.Cli.Common.Console;

class ConsolePrompt : IConsolePrompt
{
    readonly IAnsiConsole m_Console;
    public bool InteractiveEnabled { get; }
    const int k_PageSize = 10;

    public ConsolePrompt(IAnsiConsole console, bool isStandardInputRedirected)
    {
        m_Console = console;
        InteractiveEnabled = !isStandardInputRedirected;
    }

    public Task<T> PromptAsync<T>(IPrompt<T> prompt, CancellationToken cancellationToken)
        => prompt.ShowAsync(m_Console, cancellationToken);

    public Task<T> PromptAsync<T>(
        string title,
        CancellationToken cancellationToken)
    {
        var prompt = new TextPrompt<T>(title);

        return prompt.ShowAsync(m_Console, cancellationToken);
    }

    public Task<T> SelectionPromptAsync<T>(
        string title,
        ICollection<T> choices,
        CancellationToken cancellationToken,
        int pageSize = k_PageSize)
        where T : notnull
    {
        var prompt = new SelectionPrompt<T>()
            .Title(title)
            .PageSize(pageSize)
            .AddChoices(choices);

        return prompt.ShowAsync(m_Console, cancellationToken);
    }

    public Task<List<T>> MultiSelectionPromptAsync<T>(
        string title,
        ICollection<T> choices,
        CancellationToken cancellationToken,
        int pageSize = k_PageSize)
        where T : notnull
    {
        var prompt = new MultiSelectionPrompt<T>()
            .Title(title)
            .PageSize(pageSize)
            .AddChoices(choices);

        return prompt.ShowAsync(m_Console, cancellationToken);
    }

    public Task<bool> ConfirmPromptAsync(string title, CancellationToken cancellationToken)
    {
        var prompt = new ConfirmationPrompt(title);

        return prompt.ShowAsync(m_Console, cancellationToken);
    }
}
