using Unity.Services.Tooling.Editor.AccessControl.Authoring.Core.IO;

namespace Unity.Services.Cli.Access.IO;

public class FileSystem : IFileSystem
{
    public Task<string> ReadAllText(string path, CancellationToken token = default(CancellationToken))
    {
        return File.ReadAllTextAsync(path, token);
    }

    public Task WriteAllText(string path, string contents, CancellationToken token = default(CancellationToken))
    {
        return File.WriteAllTextAsync(path, contents, token);
    }

    public Task Delete(string path, CancellationToken token = default(CancellationToken))
    {
        File.Delete(path);
        return Task.CompletedTask;
    }
}
