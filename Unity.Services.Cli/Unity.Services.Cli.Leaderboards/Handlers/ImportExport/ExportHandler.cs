using Microsoft.Extensions.Logging;
using Unity.Services.Cli.Common.Console;
using Unity.Services.Cli.Authoring.Export.Input;
using Unity.Services.Cli.Leaderboards.Input;

namespace Unity.Services.Cli.Leaderboards.Handlers.ImportExport;

static class ExportHandler
{
    internal const string k_LoadingIndicatorMessage = "Exporting your environment...";
    internal const string k_CompleteIndicatorMessage = "Export complete.";

    public static async Task ExportAsync(
        ListLeaderboardInput listLeaderboardInput,
        ExportInput exportInput,
        ILogger logger,
        LeaderboardExporter? leaderboardExporter,
        ILoadingIndicator loadingIndicator,
        CancellationToken cancellationToken)
    {
        await loadingIndicator.StartLoadingAsync(
            k_LoadingIndicatorMessage,
            _ =>
            {
                leaderboardExporter!.ListLeaderboardInput = listLeaderboardInput;
                return leaderboardExporter.ExportAsync(exportInput, cancellationToken);
            }
        );
        logger.LogInformation(k_CompleteIndicatorMessage);
    }
}
