namespace Unity.Services.Cli.Common.IO;

public class FileSystem
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

    public Task Delete(string path,
        CancellationToken token = default(CancellationToken))
    {
        File.Delete(path);
        return Task.CompletedTask;
    }

    public Task<DirectoryInfo> CreateDirectory(string path)
    {
        var directoryInfo = Directory.CreateDirectory(path);
        return Task.FromResult(directoryInfo);
    }

    public Task DeleteDirectory(string path, bool recursive)
    {
        Directory.Delete(path, recursive);
        return Task.CompletedTask;
    }
}
