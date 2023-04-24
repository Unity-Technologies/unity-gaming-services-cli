using Unity.Services.Cli.Authoring.Model;
using Unity.Services.CloudCode.Authoring.Editor.Core.Model;

namespace Unity.Services.Cli.CloudCode.Authoring;

interface IJavaScriptFetchHandler
{
    public Task<FetchResult> FetchAsync(
        string rootDirectory,
        IReadOnlyList<IScript> localResources,
        bool dryRun = false,
        bool reconcile = false,
        CancellationToken token = default);
}
