namespace Unity.Services.Cli.Access.Deploy;

interface IAccessConfigLoader
{
    Task<LoadResult> LoadFilesAsync(
        IReadOnlyList<string> filePaths,
        CancellationToken token);
}
