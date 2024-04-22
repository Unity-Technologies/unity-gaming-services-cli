using Spectre.Console;

namespace Unity.Services.Cli.Common.Console;

public class LoadingIndicator : ILoadingIndicator
{
    internal readonly IAnsiConsole? AnsiConsole;

    public LoadingIndicator(IAnsiConsole? console)
    {
        AnsiConsole = console;
    }

    public async Task StartLoadingAsync(string description, Func<StatusContext?, Task> callback)
    {
        await (AnsiConsole is null ? callback(null) : StartAnsiConsoleStatusAsync(AnsiConsole, description, callback));
    }

    static async Task StartAnsiConsoleStatusAsync(IAnsiConsole console, string description, Func<StatusContext?, Task> callback) =>
        await console.Status().StartAsync(description, callback);
}
