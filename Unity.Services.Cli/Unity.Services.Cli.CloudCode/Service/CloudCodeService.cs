using System.Collections.Specialized;
using System.Diagnostics;
using System.Net;
using System.Text.RegularExpressions;
using System.Web;
using Newtonsoft.Json;
using Spectre.Console;
using Unity.Services.Cli.CloudCode.Parameters;
using Unity.Services.Cli.Common.Exceptions;
using Unity.Services.Cli.Common.Models;
using Unity.Services.Cli.Common.Validator;
using Unity.Services.Cli.ServiceAccountAuthentication;
using Unity.Services.Cli.ServiceAccountAuthentication.Token;
using Unity.Services.Gateway.CloudCodeApiV1.Generated.Api;
using Unity.Services.Gateway.CloudCodeApiV1.Generated.Client;
using Unity.Services.Gateway.CloudCodeApiV1.Generated.Model;
using Language = Unity.Services.Gateway.CloudCodeApiV1.Generated.Model.Language;

namespace Unity.Services.Cli.CloudCode.Service;

class CloudCodeService : ICloudCodeService
{
    readonly IServiceAccountAuthenticationService m_AuthenticationService;
    readonly ICloudCodeApiAsync m_CloudCodeAsyncAPI;
    readonly IConfigurationValidator m_ConfigValidator;
    readonly ICloudScriptParametersParser m_CloudScriptParametersParser;
    readonly ICloudCodeScriptParser m_CloudCodeScriptParser;

    internal const int k_ListLimit = 100;

    public CloudCodeService(ICloudCodeApiAsync cloudCodeAsyncApi, IConfigurationValidator validator,
        IServiceAccountAuthenticationService authenticationService, ICloudScriptParametersParser cloudScriptParametersParser, ICloudCodeScriptParser cloudCodeScriptParser)
    {
        m_CloudCodeAsyncAPI = cloudCodeAsyncApi;
        m_ConfigValidator = validator;
        m_AuthenticationService = authenticationService;
        m_CloudScriptParametersParser = cloudScriptParametersParser;
        m_CloudCodeScriptParser = cloudCodeScriptParser;
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
            var response = await m_CloudCodeAsyncAPI.ListScriptsAsync(projectId, environmentId, k_ListLimit, afterScript?.Name, cancellationToken: cancellationToken);
            var responseList = response.Results.ToList();
            resultsCount = responseList.Count;
            if (resultsCount < k_ListLimit)
            {
                results.AddRange(responseList);
                break;
            }

            afterScript = responseList[^1];
            results.AddRange(responseList.SkipLast(1));
        }
        while (resultsCount == k_ListLimit);

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

        await m_CloudCodeAsyncAPI
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
        var response = await m_CloudCodeAsyncAPI.PublishScriptAsync(projectId, environmentId, scriptName, payload, cancellationToken: cancellationToken);
        return response;
    }


    /// <inheritdoc cref="ICloudCodeService.GetAsync" />
    public async Task<GetScriptResponse> GetAsync(string projectId, string environmentId, string? scriptName, CancellationToken cancellationToken = default)
    {
        await AuthorizeService(cancellationToken);

        ThrowIfScriptNameInvalid(scriptName);

        m_ConfigValidator.ThrowExceptionIfConfigInvalid(Keys.ConfigKeys.ProjectId, projectId);
        m_ConfigValidator.ThrowExceptionIfConfigInvalid(Keys.ConfigKeys.EnvironmentId, environmentId);
        return await m_CloudCodeAsyncAPI.GetScriptAsync(projectId, environmentId, scriptName, cancellationToken: cancellationToken);
    }

    /// <inheritdoc cref="ICloudCodeService.CreateAsync" />
    public async Task CreateAsync(string projectId, string environmentId, string? scriptName,
        ScriptType scriptType, Language scriptLanguage, string? code, CancellationToken cancellationToken)
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

        CreateScriptRequest createScriptRequest = new CreateScriptRequest
        (
            scriptName,
            scriptType,
            await GetScriptParameters(code, cancellationToken),
            code,
            scriptLanguage
        );

        await m_CloudCodeAsyncAPI.CreateScriptAsync(projectId, environmentId, createScriptRequest, cancellationToken: cancellationToken);
    }

    /// <inheritdoc cref="ICloudCodeService.UpdateAsync" />
    public async Task UpdateAsync(string projectId, string environmentId, string? scriptName, string? code,
        CancellationToken cancellationToken)
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

        UpdateScriptRequest updateScriptRequest = new UpdateScriptRequest
        (
            await GetScriptParameters(code, cancellationToken),
            code
        );

        await m_CloudCodeAsyncAPI.UpdateScriptAsync(projectId, environmentId, scriptName, updateScriptRequest,
            cancellationToken: cancellationToken);
    }

    /// <inheritdoc cref="ICloudCodeService.UpdateModuleAsync" />
    public async Task UpdateModuleAsync(string projectId, string environmentId, string? moduleName,
        Stream moduleStream, CancellationToken cancellationToken = default)
    {
        m_ConfigValidator.ThrowExceptionIfConfigInvalid(Keys.ConfigKeys.ProjectId, projectId);
        m_ConfigValidator.ThrowExceptionIfConfigInvalid(Keys.ConfigKeys.EnvironmentId, environmentId);

        ThrowIfModuleNameInvalid(moduleName);

        await AuthorizeService(cancellationToken);

        try
        {
            await m_CloudCodeAsyncAPI.UpdateModuleAsync(
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

            await m_CloudCodeAsyncAPI.CreateModuleAsync(
                projectId,
                environmentId,
                moduleName,
                Language.CS,
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

        return await m_CloudCodeAsyncAPI.GetModuleAsync(projectId, environmentId, moduleName, cancellationToken: cancellationToken);
    }

    /// <inheritdoc cref="ICloudCodeService.DeleteModuleAsync" />
    public async Task DeleteModuleAsync(string projectId, string environmentId, string? moduleName,
        CancellationToken cancellationToken = default)
    {
        await AuthorizeService(cancellationToken);

        ThrowIfModuleNameInvalid(moduleName);

        m_ConfigValidator.ThrowExceptionIfConfigInvalid(Keys.ConfigKeys.ProjectId, projectId);
        m_ConfigValidator.ThrowExceptionIfConfigInvalid(Keys.ConfigKeys.EnvironmentId, environmentId);

        await m_CloudCodeAsyncAPI.DeleteModuleAsync(projectId, environmentId, moduleName, 0, cancellationToken: cancellationToken);
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
            var response = await m_CloudCodeAsyncAPI.ListModulesAsync(projectId, environmentId, after: pageToken, cancellationToken: cancellationToken);
            results.AddRange(response.Results.ToList());

            pageToken = response.NextPageToken;
        }
        while (pageToken != "");

        return results;
    }

    public async Task<List<ScriptParameter>> GetScriptParameters(string code, CancellationToken cancellationToken)
    {
        var parameterInJson = await m_CloudCodeScriptParser.ParseToScriptParamsJsonAsync(code, cancellationToken);
        var parameters = new List<ScriptParameter>();
        if (parameterInJson is not null)
        {
            parameters = m_CloudScriptParametersParser.ParseToScriptParameters(parameterInJson);
        }

        return parameters;
    }

    internal async Task AuthorizeService(CancellationToken cancellationToken = default)
    {
        var token = await m_AuthenticationService.GetAccessTokenAsync(cancellationToken);
        m_CloudCodeAsyncAPI.Configuration.DefaultHeaders.SetAccessTokenHeader(token);
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
}
