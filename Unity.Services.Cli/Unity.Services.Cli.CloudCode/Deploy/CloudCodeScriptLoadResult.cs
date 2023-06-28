using Unity.Services.CloudCode.Authoring.Editor.Core.Model;

namespace Unity.Services.Cli.CloudCode.Deploy;

class CloudCodeScriptLoadResult
{
    public IReadOnlyList<IScript> LoadedScripts { get; }
    public IReadOnlyList<IScript> FailedContents { get; }

    public CloudCodeScriptLoadResult(IReadOnlyList<IScript> loadedScripts, IReadOnlyList<IScript> failedContents)
    {
        LoadedScripts = loadedScripts;
        FailedContents = failedContents;
    }
}
