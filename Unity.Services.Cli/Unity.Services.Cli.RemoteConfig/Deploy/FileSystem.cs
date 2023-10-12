using Unity.Services.RemoteConfig.Editor.Authoring.Core.IO;

namespace Unity.Services.Cli.RemoteConfig.Deploy;

class FileSystem : Common.IO.FileSystem, IFileSystem
{
    public Task Delete(string path)
    {
        return base.Delete(path, CancellationToken.None);
    }
}
