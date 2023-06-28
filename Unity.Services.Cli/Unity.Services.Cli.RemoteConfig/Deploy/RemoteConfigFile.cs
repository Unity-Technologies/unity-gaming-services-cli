using Unity.Services.Cli.Authoring.Model;
using Unity.Services.DeploymentApi.Editor;
using Unity.Services.RemoteConfig.Editor.Authoring.Core.Model;

namespace Unity.Services.Cli.RemoteConfig.Deploy;

class RemoteConfigFile : DeployContent, IRemoteConfigFile
{
    public RemoteConfigFile(string name, string path)
        : base(name, "RemoteConfig File", path, 0f, DeploymentStatus.Empty )
    {
        Entries = new List<RemoteConfigEntry>();
    }
    public List<RemoteConfigEntry> Entries { get; set; }
}
