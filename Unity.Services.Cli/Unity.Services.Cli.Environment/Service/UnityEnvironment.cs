using Unity.Services.Cli.Common.Exceptions;
using Unity.Services.Cli.Common.Models;
using Unity.Services.Cli.Common.Utils;
using Unity.Services.Cli.Common.Validator;

namespace Unity.Services.Cli.Environment;

class UnityEnvironment : IUnityEnvironment
{
    public string? Name => m_Name;

    public void SetName(string? value)
    {
        m_Name = value;
    }

    public string? ProjectId => m_ProjectId;

    public void SetProjectId(string? value)
    {
        m_ProjectId = value;
    }

    readonly IConfigurationValidator m_ConfigValidator;
    readonly IEnvironmentService? m_EnvironmentService;
    string? m_Name;
    string? m_ProjectId;

    public UnityEnvironment(IEnvironmentService envService, IConfigurationValidator configurationValidator)
    {
        m_EnvironmentService = envService;
        m_ConfigValidator = configurationValidator;
    }

    /// <inheritdoc cref="IUnityEnvironment.FetchIdentifierFromSpecificEnvironmentNameAsync" />
    public async Task<string> FetchIdentifierFromSpecificEnvironmentNameAsync(string environmentName)
    {
        if (string.IsNullOrEmpty(ProjectId))
        {
            throw new MissingConfigurationException(
                Keys.ConfigKeys.ProjectId,
                Keys.EnvironmentKeys.ProjectId);
        }

        m_ConfigValidator.ThrowExceptionIfConfigInvalid(Keys.ConfigKeys.EnvironmentName, environmentName);

        var environments = await m_EnvironmentService!
            .ListAsync(ProjectId!, CancellationToken.None);

        var environmentId = environments.ToList().Find(a => a.Name == environmentName)?.Id;

        if (environmentId is null)
        {
            throw new EnvironmentNotFoundException(environmentName, ExitCode.HandledError);
        }

        return environmentId.ToString()!;
    }

    /// <inheritdoc cref="IUnityEnvironment.FetchIdentifierAsync" />
    public async Task<string> FetchIdentifierAsync()
    {
        if (string.IsNullOrEmpty(Name))
        {
            throw new MissingConfigurationException(
                Keys.ConfigKeys.EnvironmentName,
                Keys.EnvironmentKeys.EnvironmentName);
        }

        return await FetchIdentifierFromSpecificEnvironmentNameAsync(m_Name!);
    }
}
