using Unity.Services.RemoteConfig.Editor.Authoring.Core.Model;

namespace Unity.Services.Cli.RemoteConfig.Deploy;

class RemoteConfigFile : IRemoteConfigFile
{
    public RemoteConfigFile(string name, string path)
    {
        Name = name;
        Path = path;
        Entries = new List<RemoteConfigEntry>();
    }

    public string Name { get; }
    public string Path { get; set; }

    public List<RemoteConfigEntry> Entries { get; set; }
}
