using Unity.Services.Cli.Authoring.Model;
using Unity.Services.RemoteConfig.Editor.Authoring.Core.Model;

namespace Unity.Services.Cli.RemoteConfig.Deploy;

class LoadResult
{
    public IReadOnlyList<RemoteConfigFile> Loaded { get; }
    public IReadOnlyList<RemoteConfigFile> Failed { get; }

    public LoadResult(IReadOnlyList<RemoteConfigFile> loaded, IReadOnlyList<RemoteConfigFile> failed)
    {
        Loaded = loaded;
        Failed = failed;
    }
}
