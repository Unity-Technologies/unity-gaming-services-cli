using Spectre.Console;

namespace Unity.Services.Cli.Common.Console;

public class ProgressBar : IProgressBar
{
    internal readonly IAnsiConsole? k_AnsiConsole;

    readonly ProgressColumn[] k_ProgressColumns =
    {
        new TaskDescriptionColumn(),
        new ProgressBarColumn(),
        new PercentageColumn(),
        new ElapsedTimeColumn{Style = Style.Plain}
    };

    public ProgressBar(IAnsiConsole? console)
    {
        k_AnsiConsole = console;
    }

    public async Task StartProgressAsync(Func<ProgressContext?, Task> callback)
    {
        await (k_AnsiConsole is null ? callback(null) : StartAnsiConsoleProgressAsync(k_AnsiConsole, callback));
    }

    async Task StartAnsiConsoleProgressAsync(IAnsiConsole console, Func<ProgressContext?, Task> callback) =>
        await console.Progress().Columns(k_ProgressColumns).StartAsync(callback);
}
