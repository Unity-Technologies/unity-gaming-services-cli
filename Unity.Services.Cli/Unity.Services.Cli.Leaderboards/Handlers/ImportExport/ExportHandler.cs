using Microsoft.Extensions.Logging;
using Unity.Services.Cli.Common.Console;
using Unity.Services.Cli.Authoring.Export.Input;
using Unity.Services.Cli.Leaderboards.Input;

namespace Unity.Services.Cli.Leaderboards.Handlers.ImportExport;

static class ExportHandler
{
    internal const string LoadingIndicatorMessage = "Exporting your environment...";
    internal const string CompleteIndicatorMessage = "Export complete.";

    public static async Task ExportAsync(
        ExportInput exportInput,
        ILogger logger,
        LeaderboardExporter? leaderboardExporter,
        ILoadingIndicator loadingIndicator,
        CancellationToken cancellationToken)
    {
        await loadingIndicator.StartLoadingAsync(
            LoadingIndicatorMessage,
            _ =>
            {
                return leaderboardExporter!.ExportAsync(exportInput, cancellationToken);
            }
        );
        logger.LogInformation(CompleteIndicatorMessage);
    }
}
