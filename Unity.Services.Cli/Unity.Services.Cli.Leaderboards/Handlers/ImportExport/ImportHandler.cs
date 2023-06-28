using Microsoft.Extensions.Logging;
using Unity.Services.Cli.Common.Console;
using Unity.Services.Cli.Authoring.Import.Input;

namespace Unity.Services.Cli.Leaderboards.Handlers.ImportExport;

static class ImportHandler
{
    internal const string k_LoadingIndicatorMessage = "Importing configs...";
    internal const string k_CompleteIndicatorMessage = "Import complete.";

    public static async Task ImportAsync(
        ImportInput importInput,
        ILogger logger,
        LeaderboardImporter? leaderboardImporter,
        ILoadingIndicator loadingIndicator,
        CancellationToken cancellationToken
    )
    {
        await loadingIndicator.StartLoadingAsync(
            k_LoadingIndicatorMessage,
            _ => leaderboardImporter!.ImportAsync(importInput, cancellationToken));
        logger.LogInformation(k_CompleteIndicatorMessage);
    }
}
