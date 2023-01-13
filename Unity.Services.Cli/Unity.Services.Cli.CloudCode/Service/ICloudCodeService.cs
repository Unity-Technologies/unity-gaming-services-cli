using Unity.Services.Gateway.CloudCodeApiV1.Generated.Model;
using Language = Unity.Services.Gateway.CloudCodeApiV1.Generated.Model.Language;

namespace Unity.Services.Cli.CloudCode.Service;

public interface ICloudCodeService
{
    /// <summary>
    /// List a projects Cloud Code Scripts
    /// </summary>
    /// <param name="projectId">unique id of a unity project</param>
    /// <param name="environmentId">unique id of a unity environment</param>
    /// <param name="cancellationToken">token to cancel the task</param>
    /// <returns></returns>
    public Task<IEnumerable<ListScriptsResponseResultsInner>> ListAsync(string projectId, string environmentId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a script from Cloud Code
    /// </summary>
    /// <param name="projectId">unique id of a unity project</param>
    /// <param name="environmentId">unique id of a unity environment</param>
    /// <param name="scriptName">name of the script to delete</param>
    /// <param name="cancellationToken">token to cancel the task</param>
    /// <returns></returns>
    public Task DeleteAsync(string projectId, string environmentId, string? scriptName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Publish a script to be used by Clients
    /// </summary>
    /// <param name="projectId">unique id of a unity project</param>
    /// <param name="environmentId">unique id of a unity environment</param>
    /// <param name="scriptName">name of the existing script to be published</param>
    /// <param name="version">Optional: version of the script to be republished</param>
    /// <param name="cancellationToken">token to cancel the task</param>
    /// <returns></returns>
    public Task<PublishScriptResponse> PublishAsync(string projectId, string environmentId, string scriptName,
        int version = 0, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get a Cloud Code script
    /// </summary>
    /// <param name="projectId">unique id of a unity project</param>
    /// <param name="environmentId">unique id of a unity environment</param>
    /// <param name="scriptName">name of script</param>
    /// <param name="cancellationToken">token to cancel the task</param>
    /// <returns></returns>
    public Task<GetScriptResponse> GetAsync(string projectId, string environmentId, string scriptName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Create a Cloud Code script
    /// </summary>
    /// <param name="projectId">unique id of a unity project</param>
    /// <param name="environmentId">unique id of a unity environment</param>
    /// <param name="scriptName">name of script to create</param>
    /// <param name="scriptType">type of script to create</param>
    /// <param name="scriptLanguage">scripting language of script to create</param>
    /// <param name="code">code to upload to the script</param>
    /// <param name="cancellationToken">token to cancel the task</param>
    /// <returns></returns>
    public Task CreateAsync(string projectId, string environmentId, string? scriptName, ScriptType scriptType,
        Language scriptLanguage, string? code, CancellationToken cancellationToken);

    /// <summary>
    /// Update an already existing Cloud Code script
    /// </summary>
    /// <param name="projectId">unique id of a unity project</param>
    /// <param name="environmentId">unique id of a unity environment</param>
    /// <param name="scriptName">name of script to update</param>
    /// <param name="code">new code to upload to the script</param>
    /// <param name="cancellationToken">token to cancel the task</param>
    /// <returns></returns>
    public Task UpdateAsync(string projectId, string environmentId, string? scriptName, string? code,
        CancellationToken cancellationToken);

    public Task<List<ScriptParameter>> GetScriptParameters(string code, CancellationToken cancellationToken);
}
