using Unity.Services.Gateway.IdentityApiV1.Generated.Model;

namespace Unity.Services.Cli.Environment;

public interface IEnvironmentService
{
    /// <summary>
    /// List the environments of a project
    /// </summary>
    /// <param name="projectId">unique id of a unity project</param>
    /// <param name="cancellationToken">token to cancel the task</param>
    /// <returns></returns>
    public Task<IEnumerable<EnvironmentResponse>> ListAsync(string projectId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete an environment of a project
    /// </summary>
    /// <param name="projectId">unique id of a project</param>
    /// <param name="environmentId">id of the environment to delete</param>
    /// <param name="cancellationToken">token to cancel the task</param>
    /// <returns></returns>
    public Task DeleteAsync(string projectId, string environmentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Add a new environment to a project
    /// </summary>
    /// <param name="environmentName">name of a new environment to add</param>
    /// <param name="projectId">unique id of a project</param>
    /// <param name="cancellationToken">token to cancel the task</param>
    public Task AddAsync(string environmentName, string projectId, CancellationToken cancellationToken = default);
}
