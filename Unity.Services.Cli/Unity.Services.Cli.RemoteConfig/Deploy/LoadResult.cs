using Unity.Services.Cli.Authoring.Model;
using Unity.Services.RemoteConfig.Editor.Authoring.Core.Model;

namespace Unity.Services.Cli.RemoteConfig.Deploy;

class LoadResult
{
    public IEnumerable<IRemoteConfigFile> Loaded { get; }
    public IEnumerable<DeployContent> Failed { get; }

    public LoadResult(IEnumerable<IRemoteConfigFile> loaded, IEnumerable<DeployContent> failed)
    {
        Loaded = loaded;
        Failed = failed;
    }
}
