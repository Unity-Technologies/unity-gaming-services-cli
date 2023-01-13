using Unity.Services.RemoteConfig.Editor.Authoring.Core.IO;

namespace Unity.Services.Cli.RemoteConfig.Deploy;

class FileReader : IFileReader
{
    public string ReadAllText(string path)
    {
        return File.ReadAllText(path);
    }
}
