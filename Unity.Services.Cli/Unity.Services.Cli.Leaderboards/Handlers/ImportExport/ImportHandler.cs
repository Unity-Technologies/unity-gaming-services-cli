using Microsoft.Extensions.Logging;
using Unity.Services.Cli.Common.Console;
using Unity.Services.Cli.Authoring.Import.Input;

namespace Unity.Services.Cli.Leaderboards.Handlers.ImportExport;

static class ImportHandler
{
    internal const string LoadingIndicatorMessage = "Importing configs...";
    internal const string CompleteIndicatorMessage = "Import complete.";

    public static async Task ImportAsync(
        ImportInput importInput,
        ILogger logger,
        LeaderboardImporter? leaderboardImporter,
        ILoadingIndicator loadingIndicator,
        CancellationToken cancellationToken
    )
    {
        await loadingIndicator.StartLoadingAsync(
            LoadingIndicatorMessage,
            _ => leaderboardImporter!.ImportAsync(importInput, cancellationToken));
        logger.LogInformation(CompleteIndicatorMessage);
    }
}
