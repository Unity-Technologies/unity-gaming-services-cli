using Unity.Services.Cli.Common.Console;
using Unity.Services.Cli.Authoring.Import.Input;

namespace Unity.Services.Cli.RemoteConfig.Handlers.ExportImport;

static class ImportHandler
{
    internal const string LoadingIndicatorMessage = "Importing your environment...";
    public static async Task ImportAsync(
        ImportInput importInput,
        RemoteConfigImporter? remoteConfigImporter,
        ILoadingIndicator loadingIndicator,
        CancellationToken cancellationToken)
    {
        await loadingIndicator.StartLoadingAsync(
            LoadingIndicatorMessage,
            _ =>
            {
                if (remoteConfigImporter == null) return Task.CompletedTask;
                return remoteConfigImporter.ImportAsync(importInput, cancellationToken);
            }
        );
    }
}
