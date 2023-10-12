namespace Unity.Services.Cli.Access.Deploy;

public interface IAccessConfigLoader
{
    Task<LoadResult> LoadFilesAsync(
        IReadOnlyList<string> filePaths,
        CancellationToken token);
}
