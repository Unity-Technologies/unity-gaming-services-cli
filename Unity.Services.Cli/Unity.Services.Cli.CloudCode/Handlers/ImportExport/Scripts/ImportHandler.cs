using Unity.Services.Cli.Authoring.Import.Input;
using Unity.Services.Cli.Common.Console;

namespace Unity.Services.Cli.CloudCode.Handlers.ImportExport.Scripts;

static class ImportHandler
{
    internal const string k_LoadingIndicatorMessage = "Importing Cloud Code scripts...";

    public static async Task ImportAsync(
        ImportInput importInput,
        CloudCodeScriptsImporter? scriptsImporter,
        ILoadingIndicator loadingIndicator,
        CancellationToken cancellationToken)
    {
        await loadingIndicator.StartLoadingAsync(
            k_LoadingIndicatorMessage,
            _ => scriptsImporter!.ImportAsync(importInput, cancellationToken));
    }
}
