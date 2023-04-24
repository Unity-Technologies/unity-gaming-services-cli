using Unity.Services.Cli.Authoring.Model;
using Unity.Services.CloudCode.Authoring.Editor.Core.Model;

namespace Unity.Services.Cli.CloudCode.Deploy;

class CloudCodeScriptLoadResult
{
    public IReadOnlyList<IScript> LoadedScripts { get; }
    public IReadOnlyList<DeployContent> FailedContents { get; }

    public CloudCodeScriptLoadResult(IReadOnlyList<IScript> loadedScripts, IReadOnlyList<DeployContent> failedContents)
    {
        LoadedScripts = loadedScripts;
        FailedContents = failedContents;
    }
}
