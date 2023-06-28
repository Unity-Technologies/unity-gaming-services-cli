using Unity.Services.Cli.Authoring.Export.Input;
using Unity.Services.Cli.Common.Console;

namespace Unity.Services.Cli.CloudCode.Handlers.ImportExport.Modules;

static class ModuleExportHandler
{
    internal const string k_LoadingIndicatorMessage = "Exporting your environment...";

    public static async Task ExportAsync(
        ExportInput exportInput,
        CloudCodeModulesExporter? cloudCodeModulesExporter,
        ILoadingIndicator loadingIndicator,
        CancellationToken cancellationToken)
    {
        await loadingIndicator.StartLoadingAsync(
            k_LoadingIndicatorMessage,
            _ => cloudCodeModulesExporter!.ExportAsync(exportInput, cancellationToken));
    }
}
