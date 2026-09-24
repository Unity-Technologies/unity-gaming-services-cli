using System.Net;
using System.Text.RegularExpressions;
using Unity.Services.Cli.Common.Exceptions;
using Unity.Services.Cli.Common.Models;
using Unity.Services.Cli.Common.Validator;
using Unity.Services.Cli.ServiceAccountAuthentication;
using Unity.Services.Cli.ServiceAccountAuthentication.Token;
using Unity.Services.Gateway.CloudCodeApiV1.Generated.Api;
using Unity.Services.Gateway.CloudCodeApiV1.Generated.Client;
using Unity.Services.Gateway.CloudCodeApiV1.Generated.Model;

namespace Unity.Services.Cli.CloudCode.Service;

class CloudCodeService : ICloudCodeService
{
    readonly IServiceAccountAuthenticationService m_AuthenticationService;
    readonly ICloudCodeApiAsync m_CloudCodeAsyncApi;
    readonly IConfigurationValidator m_ConfigValidator;

    internal const int ListLimit = 100;
    internal const int MaxFileSizeLimitInMb = 10;

    public CloudCodeService(ICloudCodeApiAsync cloudCodeAsyncApi, IConfigurationValidator validator,
        IServiceAccountAuthenticationService authenticationService)
    {
        m_CloudCodeAsyncApi = cloudCodeAsyncApi;
        m_ConfigValidator = validator;
        m_AuthenticationService = authenticationService;
    }

    /// <inheritdoc cref="ICloudCodeService.ListAsync" />
    public async Task<IEnumerable<ListScriptsResponseResultsInner>> ListAsync(string projectId, string environmentId,
        CancellationToken cancellationToken = default)
    {
        await AuthorizeService(cancellationToken);
        m_ConfigValidator.ThrowExceptionIfConfigInvalid(Keys.ConfigKeys.ProjectId, projectId);
        m_ConfigValidator.ThrowExceptionIfConfigInvalid(Keys.ConfigKeys.EnvironmentId, environmentId);

        ListScriptsResponseResultsInner? afterScript = null;
        var results = new List<ListScriptsResponseResultsInner>();
        int resultsCount;

        do
        {
            var response = await m_CloudCodeAsyncApi.ListScriptsAsync(projectId, environmentId, ListLimit,
                afterScript?.Name, cancellationToken: cancellationToken);
            // A project with no scripts yields an empty body, so both the response and its results
            // can be null rather than an empty array.
            var responseList = response?.Results?.ToList() ?? new List<ListScriptsResponseResultsInner>();
            resultsCount = responseList.Count;
            if (resultsCount < ListLimit)
            {
                results.AddRange(responseList);
                break;
            }

            afterScript = responseList[^1];
            results.AddRange(responseList.SkipLast(1));
        } while (resultsCount == ListLimit);

        return results;
    }

    /// <inheritdoc cref="ICloudCodeService.DeleteAsync" />
    public async Task DeleteAsync(string projectId, string environmentId, string? scriptName,
        CancellationToken cancellationToken = default)
    {
        await AuthorizeService(cancellationToken);
        ThrowIfScriptNameInvalid(scriptName);
        m_ConfigValidator.ThrowExceptionIfConfigInvalid(Keys.ConfigKeys.ProjectId, projectId);
        m_ConfigValidator.ThrowExceptionIfConfigInvalid(Keys.ConfigKeys.EnvironmentId, environmentId);

        await m_CloudCodeAsyncApi
            .DeleteScriptWithHttpInfoAsync(projectId, environmentId, scriptName, cancellationToken: cancellationToken);
    }

    /// <inheritdoc cref="ICloudCodeService.PublishAsync" />
    public async Task<PublishScriptResponse> PublishAsync(string projectId, string environmentId, string scriptName,
        int version = 0, CancellationToken cancellationToken = default)
    {
        await AuthorizeService(cancellationToken);
        ThrowIfScriptNameInvalid(scriptName);
        m_ConfigValidator.ThrowExceptionIfConfigInvalid(Keys.ConfigKeys.ProjectId, projectId);
        m_ConfigValidator.ThrowExceptionIfConfigInvalid(Keys.ConfigKeys.EnvironmentId, environmentId);
        var payload = new PublishScriptRequest()
        {
            _Version = version
        };
        var response = await m_CloudCodeAsyncApi.PublishScriptAsync(projectId, environmentId, scriptName, payload,
            cancellationToken: cancellationToken);
        return response;
    }


    /// <inheritdoc cref="ICloudCodeService.GetAsync" />
    public async Task<GetScriptResponse> GetAsync(string projectId, string environmentId, string? scriptName,
        CancellationToken cancellationToken = default)
    {
        await AuthorizeService(cancellationToken);

        ThrowIfScriptNameInvalid(scriptName);

        m_ConfigValidator.ThrowExceptionIfConfigInvalid(Keys.ConfigKeys.ProjectId, projectId);
        m_ConfigValidator.ThrowExceptionIfConfigInvalid(Keys.ConfigKeys.EnvironmentId, environmentId);
        return await m_CloudCodeAsyncApi.GetScriptAsync(projectId, environmentId, scriptName,
            cancellationToken: cancellationToken);
    }

    /// <inheritdoc cref="ICloudCodeService.CreateAsync" />
    public async Task CreateAsync(string projectId, string environmentId, string? scriptName,
        string scriptType, string scriptLanguage, string? code, IReadOnlyList<ScriptParameter> scriptParameters,
        CancellationToken cancellationToken)
    {
        m_ConfigValidator.ThrowExceptionIfConfigInvalid(Keys.ConfigKeys.ProjectId, projectId);
        m_ConfigValidator.ThrowExceptionIfConfigInvalid(Keys.ConfigKeys.EnvironmentId, environmentId);

        ThrowIfScriptNameInvalid(scriptName);

        if (string.IsNullOrEmpty(code))
        {
            throw new CliException("Script could not be created because the code provided is null or empty.",
                ExitCode.HandledError);
        }

        await AuthorizeService(cancellationToken);

        var createScriptRequest = new CreateScriptRequest
        (
            scriptName,
            scriptType,
            scriptParameters.ToList(),
            code,
            scriptLanguage
        );

        await m_CloudCodeAsyncApi.CreateScriptAsync(projectId, environmentId, createScriptRequest,
            cancellationToken: cancellationToken);
    }

    /// <inheritdoc cref="ICloudCodeService.UpdateAsync" />
    public async Task UpdateAsync(string projectId, string environmentId, string? scriptName, string? code,
        IReadOnlyList<ScriptParameter> scriptParameters, CancellationToken cancellationToken)
    {
        m_ConfigValidator.ThrowExceptionIfConfigInvalid(Keys.ConfigKeys.ProjectId, projectId);
        m_ConfigValidator.ThrowExceptionIfConfigInvalid(Keys.ConfigKeys.EnvironmentId, environmentId);

        ThrowIfScriptNameInvalid(scriptName);

        if (string.IsNullOrEmpty(code))
        {
            throw new CliException("Script could not be updated because the code provided is null or empty.",
                ExitCode.HandledError);
        }

        await AuthorizeService(cancellationToken);

        var updateScriptRequest = new UpdateScriptRequest
        (
            scriptParameters.ToList(),
            code
        );

        await m_CloudCodeAsyncApi.UpdateScriptAsync(projectId, environmentId, scriptName, updateScriptRequest,
            cancellationToken: cancellationToken);
    }

    /// <inheritdoc cref="ICloudCodeService.UpdateModuleAsync" />
    public async Task UpdateModuleAsync(string projectId, string environmentId, string? moduleName,
        Stream moduleStream, CancellationToken cancellationToken = default)
    {
        m_ConfigValidator.ThrowExceptionIfConfigInvalid(Keys.ConfigKeys.ProjectId, projectId);
        m_ConfigValidator.ThrowExceptionIfConfigInvalid(Keys.ConfigKeys.EnvironmentId, environmentId);

        ThrowIfModuleNameInvalid(moduleName);
        ThrowIfFileInvalid(moduleStream);

        await AuthorizeService(cancellationToken);

        try
        {
            await m_CloudCodeAsyncApi.UpdateModuleAsync(
                projectId,
                environmentId,
                moduleName,
                moduleStream,
                cancellationToken: cancellationToken);
        }
        catch (ApiException ex)
            when ((HttpStatusCode)ex.ErrorCode == HttpStatusCode.NotFound)
        {
            moduleStream.Seek(0, SeekOrigin.Begin);

            await m_CloudCodeAsyncApi.CreateModuleAsync(
                projectId,
                environmentId,
                moduleName,
                "CS",
                moduleStream,
                cancellationToken: cancellationToken);
        }
    }

    /// <inheritdoc cref="ICloudCodeService.GetModuleAsync" />
    public async Task<GetModuleResponse> GetModuleAsync(string projectId, string environmentId, string moduleName,
        CancellationToken cancellationToken = default)
    {
        await AuthorizeService(cancellationToken);
        ThrowIfModuleNameInvalid(moduleName);

        m_ConfigValidator.ThrowExceptionIfConfigInvalid(Keys.ConfigKeys.ProjectId, projectId);
        m_ConfigValidator.ThrowExceptionIfConfigInvalid(Keys.ConfigKeys.EnvironmentId, environmentId);

        return await m_CloudCodeAsyncApi.GetModuleAsync(projectId, environmentId, moduleName, cancellationToken: cancellationToken);
    }

    /// <inheritdoc cref="ICloudCodeService.DeleteModuleAsync" />
    public async Task DeleteModuleAsync(string projectId, string environmentId, string? moduleName,
        CancellationToken cancellationToken = default)
    {
        await AuthorizeService(cancellationToken);

        ThrowIfModuleNameInvalid(moduleName);

        m_ConfigValidator.ThrowExceptionIfConfigInvalid(Keys.ConfigKeys.ProjectId, projectId);
        m_ConfigValidator.ThrowExceptionIfConfigInvalid(Keys.ConfigKeys.EnvironmentId, environmentId);

        await m_CloudCodeAsyncApi.DeleteModuleAsync(projectId, environmentId, moduleName, cancellationToken: cancellationToken);
    }

    /// <inheritdoc cref="ICloudCodeService.DeleteModuleVersionAsync" />
    public async Task DeleteModuleVersionAsync(string projectId, string environmentId, string? moduleName,
        long version, CancellationToken cancellationToken = default)
    {
        await AuthorizeService(cancellationToken);

        ThrowIfModuleNameInvalid(moduleName);

        m_ConfigValidator.ThrowExceptionIfConfigInvalid(Keys.ConfigKeys.ProjectId, projectId);
        m_ConfigValidator.ThrowExceptionIfConfigInvalid(Keys.ConfigKeys.EnvironmentId, environmentId);

        try
        {
            await m_CloudCodeAsyncApi.DeleteModuleVersionAsync(
                projectId,
                environmentId,
                moduleName,
                version,
                cancellationToken: cancellationToken);
        }
        catch (ApiException ex)
        {
            // Each status maps to a distinct message. The wording follows the API's own vocabulary:
            // a module has a "live" version, where a script has an "active" one.
            throw (HttpStatusCode)ex.ErrorCode switch
            {
                HttpStatusCode.BadRequest => new CliException(
                    $"'{version}' is not a valid module version. Provide a version number as listed by"
                    + $" 'ugs cloud-code modules get {moduleName} --versions'.", ExitCode.HandledError),
                HttpStatusCode.Forbidden => new CliException(
                    "Module versions are not enabled for this project, so there is no version to delete.",
                    ExitCode.HandledError),
                HttpStatusCode.NotFound => new CliException(
                    $"Version '{version}' of module '{moduleName}' could not be found.", ExitCode.HandledError),
                HttpStatusCode.Conflict => new CliException(
                    $"Version '{version}' of module '{moduleName}' cannot be deleted because it is the live"
                    + " version or is assigned to a release.", ExitCode.HandledError),
                _ => ex
            };
        }
    }

    /// <inheritdoc cref="ICloudCodeService.DeleteScriptVersionAsync" />
    public async Task DeleteScriptVersionAsync(string projectId, string environmentId, string? scriptName,
        int version, CancellationToken cancellationToken = default)
    {
        await AuthorizeService(cancellationToken);

        ThrowIfScriptNameInvalid(scriptName);

        m_ConfigValidator.ThrowExceptionIfConfigInvalid(Keys.ConfigKeys.ProjectId, projectId);
        m_ConfigValidator.ThrowExceptionIfConfigInvalid(Keys.ConfigKeys.EnvironmentId, environmentId);

        try
        {
            await m_CloudCodeAsyncApi.DeleteScriptVersionAsync(
                projectId,
                environmentId,
                scriptName,
                version,
                cancellationToken: cancellationToken);
        }
        catch (ApiException ex)
        {
            // No Forbidden case: unlike modules, the script version route is not gated on the module
            // versions feature flag, so a 403 cannot arise here.
            throw (HttpStatusCode)ex.ErrorCode switch
            {
                HttpStatusCode.BadRequest => new CliException(
                    $"'{version}' is not a valid script version. Provide a version number as listed by"
                    + $" 'ugs cloud-code scripts get {scriptName} --versions'.", ExitCode.HandledError),
                HttpStatusCode.NotFound => new CliException(
                    $"Version '{version}' of script '{scriptName}' could not be found.", ExitCode.HandledError),
                HttpStatusCode.Conflict => new CliException(
                    $"Version '{version}' of script '{scriptName}' cannot be deleted because it is the active"
                    + " version or is assigned to a release.", ExitCode.HandledError),
                _ => ex
            };
        }
    }

    /// <inheritdoc cref="ICloudCodeService.GetModuleSpecAsync" />
    public async Task<string> GetModuleSpecAsync(string projectId, string environmentId, string moduleName,
        string? version = null, CancellationToken cancellationToken = default)
    {
        await AuthorizeService(cancellationToken);
        ThrowIfModuleNameInvalid(moduleName);

        m_ConfigValidator.ThrowExceptionIfConfigInvalid(Keys.ConfigKeys.ProjectId, projectId);
        m_ConfigValidator.ThrowExceptionIfConfigInvalid(Keys.ConfigKeys.EnvironmentId, environmentId);

        return await m_CloudCodeAsyncApi.ModuleSpecAsync(
            projectId,
            environmentId,
            moduleName,
            version: version,
            cancellationToken: cancellationToken);
    }

    public async Task<IEnumerable<ListModulesResponseResultsInner>> ListModulesAsync(string projectId, string environmentId,
        CancellationToken cancellationToken = default)
    {
        await AuthorizeService(cancellationToken);
        m_ConfigValidator.ThrowExceptionIfConfigInvalid(Keys.ConfigKeys.ProjectId, projectId);
        m_ConfigValidator.ThrowExceptionIfConfigInvalid(Keys.ConfigKeys.EnvironmentId, environmentId);

        var results = new List<ListModulesResponseResultsInner>();
        var pageToken = "";
        do
        {
            var response = await m_CloudCodeAsyncApi.ListModulesAsync(projectId, environmentId, after: pageToken, cancellationToken: cancellationToken);
            // A project with no modules yields an empty body, so both the response and its results
            // can be null rather than an empty array.
            results.AddRange(response?.Results ?? new List<ListModulesResponseResultsInner>());

            // A null token would never equal "" and so would loop forever.
            pageToken = response?.NextPageToken ?? "";
        }
        while (pageToken != "");

        return results;
    }

    internal async Task AuthorizeService(CancellationToken cancellationToken = default)
    {
        var token = await m_AuthenticationService.GetAccessTokenAsync(cancellationToken);
        m_CloudCodeAsyncApi.Configuration.DefaultHeaders.SetAccessTokenHeader(token);
    }

    static void ThrowIfScriptNameInvalid(string? scriptName)
    {
        if (!RegexWithTimeout(scriptName, "^[a-zA-Z0-9-_]+$"))
        {
            throw new CliException($"{scriptName} is not a valid script name. A valid script name must" +
                                   " only contain letters, numbers, underscores and dashes.", ExitCode.HandledError);
        }
    }

    static void ThrowIfModuleNameInvalid(string? moduleName)
    {
        if (!RegexWithTimeout(moduleName, "^[a-zA-Z0-9_]+$"))
        {
            throw new CliException($"{moduleName} is not a valid module name. A valid module name must" +
                                   " only contain letters, numbers and underscores.", ExitCode.HandledError);
        }
    }

    static bool RegexWithTimeout(string? input, string pattern, int timeoutSeconds = 2)
    {
        try
        {
            if (String.IsNullOrEmpty(input) ||
                !Regex.IsMatch(input, pattern, RegexOptions.None, TimeSpan.FromSeconds(timeoutSeconds)))
            {
                return false;
            }
        }
        catch (RegexMatchTimeoutException)
        {
            return false;
        }
        return true;
    }

    static void ThrowIfFileInvalid(Stream stream)
    {
        if (stream == null || stream.Length == 0)
        {
            throw new CliException("Module could not be updated because the file provided is null or empty.",
                ExitCode.HandledError);
        }

        if (stream.Length > 1024 * 1024 * MaxFileSizeLimitInMb)
        {
            throw new CliException($"Module could not be updated because the file provided is over the size limit of {MaxFileSizeLimitInMb}MB.",
                ExitCode.HandledError);
        }
    }
}
