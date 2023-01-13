using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using Unity.Services.Cli.Common.Exceptions;
using Unity.Services.Cli.Common.Models;
using Unity.Services.Cli.Common.Networking;
using Unity.Services.Cli.Common.Validator;
using Unity.Services.Cli.RemoteConfig.Model;
using Unity.Services.Cli.RemoteConfig.Types;
using Unity.Services.Cli.ServiceAccountAuthentication;

namespace Unity.Services.Cli.RemoteConfig.Service;

public class RemoteConfigService : IRemoteConfigService
{
    const string k_ConfigTypeDefaultValue = "settings";

    static readonly string k_BaseUrl = $"{EndpointHelper.GetCurrentEndpointFor<RemoteConfigEndpoints>()}/remote-config/v1";

    static readonly JsonSerializerSettings k_JsonSerializerSettings = new ()
    {
        ContractResolver = new CamelCasePropertyNamesContractResolver()
    };

    readonly HttpClient m_httpClient = new();
    readonly IServiceAccountAuthenticationService m_authService;
    readonly IConfigurationValidator m_configValidator;

    public RemoteConfigService(IServiceAccountAuthenticationService authService, IConfigurationValidator validator)
    {
        m_authService = authService;
        m_configValidator = validator;
        m_httpClient.DefaultRequestHeaders.SetXClientIdHeader();
    }

    public Task<string> CreateConfigAsync(
        string projectId,
        string environmentId,
        string? configType,
        IEnumerable<ConfigValue> values,
        CancellationToken cancellationToken = default)
    {
        var body = CreateBody(configType, values);
        body.Add("environmentId", environmentId);

        return CreateConfigAsync(projectId, body.ToString(), cancellationToken);
    }

    async Task<string> CreateConfigAsync(string projectId, string body, CancellationToken cancellationToken = default)
    {
        m_configValidator.ThrowExceptionIfConfigInvalid(Keys.ConfigKeys.ProjectId, projectId);

        try
        {
            await AuthorizeServiceAsync(cancellationToken);
            var utf8Body = Encoding.UTF8.GetString(Encoding.Default.GetBytes(body));
            var response = await m_httpClient.PostAsync(
                $"{k_BaseUrl}/projects/{projectId}/configs",
                new StringContent(utf8Body, Encoding.UTF8, "application/json"),
                cancellationToken);
            response.EnsureSuccessStatusCode();
            var responseStr = await response.Content.ReadAsStringAsync(cancellationToken);
            var responseObj = JsonConvert.DeserializeObject<CreateResponse>(responseStr, k_JsonSerializerSettings);
            return responseObj?.Id!;
        }
        catch (HttpRequestException exception)
        {
            throw new CliException($"{nameof(CreateConfigAsync)} failed: {exception.Message}", exception, ExitCode.UnhandledError);
        }
    }

    public Task UpdateConfigAsync(string projectId, string configId, string? configType, IEnumerable<ConfigValue> values, CancellationToken cancellationToken = default)
    {
        var body = CreateBody(configType, values);

        return UpdateConfigAsync(projectId, configId, body.ToString(), cancellationToken);
    }

    public Task UpdateConfigAsync(string projectId, string configId, string body, CancellationToken cancellationToken = default)
    {
        m_configValidator.ThrowExceptionIfConfigInvalid(Keys.ConfigKeys.ProjectId, projectId);
        if (string.IsNullOrEmpty(configId))
        {
            throw new ArgumentException("Required parameter is invalid", nameof(configId));
        }

        return UpdateConfigInternalAsync(projectId, configId, body, cancellationToken);
    }

    private async Task UpdateConfigInternalAsync(string projectId, string configId, string body, CancellationToken cancellationToken = default)
    {
        try
        {
            await AuthorizeServiceAsync(cancellationToken);
            var utf8Body = Encoding.UTF8.GetString(Encoding.Default.GetBytes(body));
            const string deserializationErrorMsg = "Failed to deserialize config update request body.";
            UpdateConfigRequest? updateRequest;

            // Deserializing the body here is necessary in order to check the config type.
            try
            {
                updateRequest = JsonConvert.DeserializeObject<UpdateConfigRequest>(utf8Body, k_JsonSerializerSettings);
            }
            catch (Exception ex)
            {
                throw new CliException(deserializationErrorMsg, ex, ExitCode.HandledError);
            }

            if (updateRequest is null || string.IsNullOrEmpty(updateRequest.Type))
            {
                throw new CliException(deserializationErrorMsg, null, ExitCode.HandledError);
            }

            var uri = new Uri($"{k_BaseUrl}/projects/{projectId}/configs/{configId}");
            var request = new HttpRequestMessage(HttpMethod.Put, uri);
            request.Content = new StringContent(utf8Body, Encoding.UTF8, "application/json");
            if (updateRequest.Type != k_ConfigTypeDefaultValue)
            {
                // Only set x-requesting-service if a non-default configType was specified
                request.Headers.Add("x-requesting-service", updateRequest.Type);
            }
            var response = await m_httpClient.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();
        }
        catch (HttpRequestException exception)
        {
            throw new CliException($"{nameof(UpdateConfigInternalAsync)} failed: {exception.Message}", exception, ExitCode.HandledError);
        }
    }

    public Task<string> GetAllConfigsFromEnvironmentAsync(string projectId, string? environmentId, string? configType, CancellationToken cancellationToken = default)
    {
        m_configValidator.ThrowExceptionIfConfigInvalid(Keys.ConfigKeys.ProjectId, projectId);
        return GetAllConfigsFromEnvironmentInternalAsync(projectId, environmentId, configType, cancellationToken);
    }

    private async Task<string> GetAllConfigsFromEnvironmentInternalAsync(string projectId, string? environmentId, string? configType, CancellationToken cancellationToken = default)
    {
        try
        {
            var sb = new StringBuilder($"/projects/{projectId}");
            // environmentId is an optional parameter, only add it if it's present
            if (m_configValidator.IsConfigValid(Keys.ConfigKeys.EnvironmentId, environmentId!, out _))
            {
                sb.Append($"/environments/{environmentId}");
            }

            await AuthorizeServiceAsync(cancellationToken);

            var uri = new Uri($"{k_BaseUrl}{sb}/configs");
            var request = new HttpRequestMessage(HttpMethod.Get, uri);
            if (!string.IsNullOrEmpty(configType) && configType != k_ConfigTypeDefaultValue)
            {
                // Only set x-requesting-service if a non-default configType was specified
                request.Headers.Add("x-requesting-service", configType);
            }
            var response = await m_httpClient.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync(cancellationToken);
        }
        catch (HttpRequestException exception)
        {
            throw new CliException($"{nameof(GetAllConfigsFromEnvironmentAsync)} failed: {exception.Message}", exception, ExitCode.HandledError);
        }
    }

    static JObject CreateBody(string? configType, IEnumerable<ConfigValue> values)
    {
        var body = new JObject(
            new JProperty("type", configType ?? k_ConfigTypeDefaultValue),
            new JProperty("value",
                new JArray(values.Select(value =>
                    new JObject(
                        new JProperty("key", value.Key),
                        new JProperty("type", value.Type.ToString().ToLower()),
                        new JProperty("value", value.Value)
                    ))
            ))
        );
        return body;
    }

    private async Task AuthorizeServiceAsync(CancellationToken cancellationToken = default)
    {
        var token = await m_authService.GetAccessTokenAsync(cancellationToken);
        m_httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", token);
    }
}
