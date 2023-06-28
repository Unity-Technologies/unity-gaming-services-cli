using Unity.Services.Cli.Common.Console;
using Unity.Services.Cli.Authoring.Export.Input;

namespace Unity.Services.Cli.RemoteConfig.Handlers.ExportImport;

static class ExportHandler
{
    internal const string LoadingIndicatorMessage = "Exporting your environment...";
    public static async Task ExportAsync(
        ExportInput exportInput,
        RemoteConfigExporter? remoteConfigExporter,
        ILoadingIndicator loadingIndicator,
        CancellationToken cancellationToken)
    {
        await loadingIndicator.StartLoadingAsync(
            LoadingIndicatorMessage,
            _ =>
            {
                if (remoteConfigExporter == null) return Task.CompletedTask;
                return remoteConfigExporter.ExportAsync(exportInput, cancellationToken);
            }
        );
    }
}
