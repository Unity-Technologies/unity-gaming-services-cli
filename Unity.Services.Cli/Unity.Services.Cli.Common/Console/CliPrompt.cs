using Spectre.Console;

namespace Unity.Services.Cli.Common.Console;

class CliPrompt : ICliPrompt
{
    readonly IAnsiConsole m_Console;
    public bool IsStandardInputRedirected { get; }

    public CliPrompt(IAnsiConsole console, bool isStandardInputRedirected)
    {
        m_Console = console;
        IsStandardInputRedirected = isStandardInputRedirected;
    }

    public Task<T> PromptAsync<T>(IPrompt<T> prompt, CancellationToken cancellationToken)
        => prompt.ShowAsync(m_Console, cancellationToken);
}
