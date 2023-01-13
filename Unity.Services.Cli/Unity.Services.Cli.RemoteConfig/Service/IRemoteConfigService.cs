using Unity.Services.Cli.RemoteConfig.Types;

namespace Unity.Services.Cli.RemoteConfig.Service;

public interface IRemoteConfigService
{
    public Task<string> CreateConfigAsync(
        string projectId,
        string environmentId,
        string configType,
        IEnumerable<ConfigValue> values,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Update an existing config, constructing the payload from the values provided.
    /// </summary>
    /// <param name="projectId">Unity Genesis project ID</param>
    /// <param name="configId">ID of the config being updated</param>
    /// <param name="configType">Type of the config being updated (default: "settings")</param>
    /// <param name="values">Collection of config values</param>
    /// <param name="cancellationToken">Token to cancel the task</param>
    public Task UpdateConfigAsync(string projectId, string configId, string? configType, IEnumerable<ConfigValue> values, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update an existing config from the provided JSON string.
    /// </summary>
    /// <param name="projectId">Unity Genesis project ID</param>
    /// <param name="configId">ID of the config being updated</param>
    /// <param name="body">Serialized JSON body containing the <a href="https://services.docs.unity.com/remote-config-admin/v1#tag/Configs/paths/~1remote-config~1v1~1projects~1%7BprojectId%7D~1configs~1%7BconfigId%7D/put">update</a></param>
    /// <param name="cancellationToken">Token to cancel the task</param>
    public Task UpdateConfigAsync(string projectId, string configId, string body, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all configs from the provided environment.
    /// </summary>
    /// <param name="projectId">Unity Genesis project ID</param>
    /// <param name="environmentId">ID of the environment, or default environment if not provided</param>
    /// <param name="configType">Type of the config being updated (default: "settings")</param>
    /// <param name="cancellationToken">Token to cancel the task</param>
    public Task<string> GetAllConfigsFromEnvironmentAsync(string projectId, string? environmentId, string? configType, CancellationToken cancellationToken = default);
}
