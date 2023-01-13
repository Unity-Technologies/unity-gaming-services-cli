using Spectre.Console;

namespace Unity.Services.Cli.Common.Console;

class CliPrompt : ICliPrompt
{
    readonly IAnsiConsole m_Console;

    public CliPrompt(IAnsiConsole console)
    {
        m_Console = console;
    }

    public Task<T> PromptAsync<T>(IPrompt<T> prompt, CancellationToken cancellationToken)
        => prompt.ShowAsync(m_Console, cancellationToken);
}
