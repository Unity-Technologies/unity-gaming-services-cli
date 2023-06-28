using Unity.Services.Cli.Common.Console;
using Unity.Services.Cli.Authoring.Export.Input;

namespace Unity.Services.Cli.Lobby.Handlers.ImportExport;

static class ExportHandler
{
    internal const string LoadingIndicatorMessage = "Exporting your environment...";

    public static async Task ExportAsync(
        ExportInput exportInput,
        LobbyExporter? lobbyExporter,
        ILoadingIndicator loadingIndicator,
        CancellationToken cancellationToken)
    {
        await loadingIndicator.StartLoadingAsync(
            LoadingIndicatorMessage,
            _ =>
            {
                return lobbyExporter!.ExportAsync(exportInput, cancellationToken);
            }
        );
    }
}
