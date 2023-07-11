namespace Unity.Services.Cli.GameServerHosting.Services;

interface IDeployFileService
{
    public IEnumerable<string> ListFilesToDeploy(ICollection<string> paths, string extension);
    public Task<string> ReadAllTextAsync(string path, CancellationToken cancellationToken);
}
