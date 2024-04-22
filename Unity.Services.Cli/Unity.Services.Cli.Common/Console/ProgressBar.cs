using Spectre.Console;

namespace Unity.Services.Cli.Common.Console;

public class ProgressBar : IProgressBar
{
    internal readonly IAnsiConsole? AnsiConsole;

    readonly ProgressColumn[] m_ProgressColumns =
    {
        new TaskDescriptionColumn(),
        new ProgressBarColumn(),
        new PercentageColumn(),
        new ElapsedTimeColumn{Style = Style.Plain}
    };

    public ProgressBar(IAnsiConsole? console)
    {
        AnsiConsole = console;
    }

    public async Task StartProgressAsync(Func<ProgressContext?, Task> callback)
    {
        await (AnsiConsole is null ? callback(null) : StartAnsiConsoleProgressAsync(AnsiConsole, callback));
    }

    async Task StartAnsiConsoleProgressAsync(IAnsiConsole console, Func<ProgressContext?, Task> callback) =>
        await console.Progress().Columns(m_ProgressColumns).StartAsync(callback);
}
