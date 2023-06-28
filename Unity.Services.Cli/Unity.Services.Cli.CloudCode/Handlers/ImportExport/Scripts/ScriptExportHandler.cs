using Unity.Services.Cli.Authoring.Export.Input;
using Unity.Services.Cli.Common.Console;

namespace Unity.Services.Cli.CloudCode.Handlers.ImportExport.Scripts;

static class ScriptExportHandler
{
    internal const string k_LoadingIndicatorMessage = "Exporting your environment...";

    public static async Task ExportAsync(
        ExportInput exportInput,
        CloudCodeScriptsExporter? cloudCodeScriptsExporter,
        ILoadingIndicator loadingIndicator,
        CancellationToken cancellationToken)
    {
        await loadingIndicator.StartLoadingAsync(
            k_LoadingIndicatorMessage,
            _ => cloudCodeScriptsExporter!.ExportAsync(exportInput, cancellationToken));
    }
}
