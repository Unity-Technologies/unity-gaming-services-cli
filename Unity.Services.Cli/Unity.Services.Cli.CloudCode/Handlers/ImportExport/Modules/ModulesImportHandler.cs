using Unity.Services.Cli.Authoring.Import.Input;
using Unity.Services.Cli.Common.Console;

namespace Unity.Services.Cli.CloudCode.Handlers.ImportExport.Modules;
static class ModulesImportHandler
{
    internal const string k_LoadingIndicatorMessage = "Importing Cloud Code modules...";

    public static async Task ImportAsync(
        ImportInput importInput,
        CloudCodeModulesImporter? modulesImporter,
        ILoadingIndicator loadingIndicator,
        CancellationToken cancellationToken
    )
    {
        await loadingIndicator.StartLoadingAsync(
            k_LoadingIndicatorMessage,
            _ => modulesImporter!.ImportAsync(importInput, cancellationToken));
    }
}
