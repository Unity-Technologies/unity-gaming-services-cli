using Spectre.Console;

namespace Unity.Services.Cli.Common.Console;

public class LoadingIndicator : ILoadingIndicator
{
    internal readonly IAnsiConsole? k_AnsiConsole;

    public LoadingIndicator(IAnsiConsole? console)
    {
        k_AnsiConsole = console;
    }

    public async Task StartLoadingAsync(string description, Func<StatusContext?, Task> callback)
    {
        await (k_AnsiConsole is null ? callback(null) : StartAnsiConsoleStatusAsync(k_AnsiConsole, description, callback));
    }

    static async Task StartAnsiConsoleStatusAsync(IAnsiConsole console, string description, Func<StatusContext?, Task> callback) =>
        await console.Status().StartAsync(description, callback);
}
