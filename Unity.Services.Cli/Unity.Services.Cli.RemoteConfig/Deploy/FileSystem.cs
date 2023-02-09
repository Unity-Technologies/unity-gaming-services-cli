using Unity.Services.RemoteConfig.Editor.Authoring.Core.IO;

namespace Unity.Services.Cli.RemoteConfig.Deploy;

class FileSystem : IFileSystem
{
    public Task<string> ReadAllText(
        string path,
        CancellationToken token = new CancellationToken())
    {
        return File.ReadAllTextAsync(path, token);
    }

    public Task WriteAllText(
        string path,
        string contents,
        CancellationToken token = new CancellationToken())
    {
        return File.WriteAllTextAsync(path, contents, token);
    }
}
