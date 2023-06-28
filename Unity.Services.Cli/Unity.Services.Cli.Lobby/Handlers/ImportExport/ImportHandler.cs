using Unity.Services.Cli.Common.Console;
using Unity.Services.Cli.Authoring.Import.Input;

namespace Unity.Services.Cli.Lobby.Handlers.ImportExport;

static class ImportHandler
{
    internal const string LoadingIndicatorMessage = "Importing configs...";

    public static async Task ImportAsync(
        ImportInput importInput,
        LobbyImporter? lobbyImporter,
        ILoadingIndicator loadingIndicator,
        CancellationToken cancellationToken
    )
    {
        await loadingIndicator.StartLoadingAsync(
            LoadingIndicatorMessage,
            _ => lobbyImporter!.ImportAsync(importInput, cancellationToken));
    }
}
