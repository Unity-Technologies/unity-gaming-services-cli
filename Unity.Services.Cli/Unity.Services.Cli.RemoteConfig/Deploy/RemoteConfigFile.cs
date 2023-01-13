using Unity.Services.RemoteConfig.Editor.Authoring.Core.Model;

namespace Unity.Services.Cli.RemoteConfig.Deploy;

class RemoteConfigFile : IRemoteConfigFile
{
    public RemoteConfigFile(string name, string path, RemoteConfigFileContent content)
    {
        Name = name;
        Path = path;
        Content = content;
    }

    public string Name { get; }
    public string Path { get; set; }
    public RemoteConfigFileContent Content { get; set; }
}
