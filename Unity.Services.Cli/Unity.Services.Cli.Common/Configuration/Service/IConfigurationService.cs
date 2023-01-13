namespace Unity.Services.Cli.Common;

public interface IConfigurationService
{
    public Task SetConfigArgumentsAsync(string key, string value, CancellationToken cancellationToken = default);
    public Task<string?> GetConfigArgumentsAsync(string key, CancellationToken cancellationToken = default);
    public Task DeleteConfigArgumentsAsync(string[] keys, CancellationToken cancellationToken = default);
}
