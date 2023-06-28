using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using Unity.Services.Cli.Common.Exceptions;
using Unity.Services.Cli.Common.Models;
using Unity.Services.Cli.Common.Networking;
using Unity.Services.Cli.Common.Validator;
using Unity.Services.Cli.RemoteConfig.Exceptions;
using Unity.Services.Cli.RemoteConfig.Model;
using Unity.Services.Cli.RemoteConfig.Types;
using Unity.Services.Cli.ServiceAccountAuthentication;

namespace Unity.Services.Cli.RemoteConfig.Service;

public class RemoteConfigService : IRemoteConfigService
{
    internal const string k_ConfigTypeDefaultValue = "settings";

    static readonly string k_BaseUrl = $"{EndpointHelper.GetCurrentEndpointFor<RemoteConfigEndpoints>()}/remote-config/v1";

    static readonly string k_InternalBaseUrl = $"{EndpointHelper.GetCurrentEndpointFor<RemoteConfigInternalEndpoints>()}/api/remote-config/v1";

    static readonly JsonSerializerSettings k_JsonSerializerSettings = new()
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

    /// <inheritdoc />
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

        string requestType = GetRequestTypeFromBody(body);

        var uri = new Uri($"{k_BaseUrl}/projects/{projectId}/configs");
        var request = new HttpRequestMessage(HttpMethod.Post, uri)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        };
        if (requestType != k_ConfigTypeDefaultValue)
        {
            // Only set x-requesting-service if a non-default configType was specified
            request.Headers.Add("x-requesting-service", requestType);
        }

        await AuthorizeServiceAsync(cancellationToken);

        HttpResponseMessage response = new HttpResponseMessage();
        try
        {
            response = await m_httpClient.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();
            var responseStr = await response.Content.ReadAsStringAsync(cancellationToken);
            var responseObj = JsonConvert.DeserializeObject<CreateResponse>(responseStr, k_JsonSerializerSettings);
            return responseObj?.Id!;
        }
        catch (HttpRequestException exception)
        {
            string content = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new ApiException($"{nameof(CreateConfigAsync)} failed: {content}", exception, ExitCode.UnhandledError);
        }
    }

    /// <inheritdoc />
    public Task UpdateConfigAsync(string projectId,
        string configId,
        string? configType,
        IEnumerable<ConfigValue> values,
        CancellationToken cancellationToken = default)
    {
        var body = CreateBody(configType, values);

        return UpdateConfigAsync(projectId, configId, body.ToString(), cancellationToken);
    }

    /// <inheritdoc />
    public Task UpdateConfigAsync(string projectId,
        string configId,
        string body,
        CancellationToken cancellationToken = default)
    {
        m_configValidator.ThrowExceptionIfConfigInvalid(Keys.ConfigKeys.ProjectId, projectId);
        if (string.IsNullOrEmpty(configId))
        {
            throw new ArgumentException("Required parameter is invalid", nameof(configId));
        }

        return UpdateConfigInternalAsync(projectId, configId, body, cancellationToken);
    }

    async Task UpdateConfigInternalAsync(string projectId,
        string configId,
        string body,
        CancellationToken cancellationToken = default)
    {
        m_configValidator.ThrowExceptionIfConfigInvalid(Keys.ConfigKeys.ProjectId, projectId);

        var requestType = GetRequestTypeFromBody(body);

        var uri = new Uri($"{k_BaseUrl}/projects/{projectId}/configs/{configId}");
        var request = new HttpRequestMessage(HttpMethod.Put, uri)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        };
        if (requestType != k_ConfigTypeDefaultValue)
        {
            // Only set x-requesting-service if a non-default configType was specified
            request.Headers.Add("x-requesting-service", requestType);
        }

        await AuthorizeServiceAsync(cancellationToken);

        HttpResponseMessage response = new HttpResponseMessage();
        try
        {
            response = await m_httpClient.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();
        }
        catch (HttpRequestException exception)
        {
            string content = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new ApiException($"{nameof(UpdateConfigInternalAsync)} failed: {content}", exception, ExitCode.UnhandledError);
        }
    }

    /// <inheritdoc />
    public async Task DeleteConfigAsync(string projectId,
        string configId,
        string? configType,
        CancellationToken cancellationToken = default)
    {
        m_configValidator.ThrowExceptionIfConfigInvalid(Keys.ConfigKeys.ProjectId, projectId);
        if (string.IsNullOrEmpty(configId))
        {
            throw new ArgumentException("Required parameter is invalid", nameof(configId));
        }

        try
        {
            await AuthorizeServiceAsync(cancellationToken);

            var uri = new Uri($"{k_BaseUrl}/projects/{projectId}/configs/{configId}");
            var request = new HttpRequestMessage(HttpMethod.Delete, uri);

            if (configType != null && configType != k_ConfigTypeDefaultValue)
            {
                // Only set x-requesting-service if a non-default configType was specified
                request.Headers.Add("x-requesting-service", configType);
            }

            var response = await m_httpClient.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();
        }
        catch (HttpRequestException exception)
        {
            throw new ApiException($"{nameof(DeleteConfigAsync)} failed", exception, ExitCode.UnhandledError);
        }
    }

    /// <inheritdoc />
    public Task<string> GetAllConfigsFromEnvironmentAsync(string projectId,
        string? environmentId,
        string? configType,
        CancellationToken cancellationToken = default)
    {
        m_configValidator.ThrowExceptionIfConfigInvalid(Keys.ConfigKeys.ProjectId, projectId);
        return GetAllConfigsFromEnvironmentInternalAsync(projectId, environmentId, configType, cancellationToken);
    }

    async Task<string> GetAllConfigsFromEnvironmentInternalAsync(string projectId,
        string? environmentId,
        string? configType,
        CancellationToken cancellationToken = default)
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
            throw new ApiException($"{nameof(GetAllConfigsFromEnvironmentAsync)} failed", exception, ExitCode.UnhandledError);
        }
    }

    public async Task ApplySchemaAsync(string projectId,
        string configId,
        string body,
        CancellationToken cancellationToken = default)
    {
        m_configValidator.ThrowExceptionIfConfigInvalid(Keys.ConfigKeys.ProjectId, projectId);
        if (string.IsNullOrEmpty(configId))
        {
            throw new ArgumentException("Required parameter is invalid", nameof(configId));
        }

        HttpResponseMessage response = new HttpResponseMessage();
        try
        {
            await AuthorizeServiceAsync(cancellationToken);

            var uri = new Uri($"{k_InternalBaseUrl}/projects/{projectId}/configs/{configId}/schemas");
            var request = new HttpRequestMessage(HttpMethod.Post, uri)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json"),
            };

            response = await m_httpClient.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();
        }
        catch (HttpRequestException exception)
        {
            // A 409 Conflict is returned if the exact same schema has already been applied to this config, so we
            // consider that to be successful too.
            if (response.StatusCode == System.Net.HttpStatusCode.Conflict)
            {
                return;
            }

            throw new ApiException($"{nameof(ApplySchemaAsync)} failed", exception, ExitCode.UnhandledError);
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

    /// <summary>
    /// Gets the type property from the Remote Config request bod.
    /// </summary>
    /// <param name="body">The JSON formatted request body to extract the type from.</param>
    /// <returns>The type from the request body if it's valid.</returns>
    /// <exception cref="CliException">Throws if the request body is invalid or doesn't contain a type.</exception>
    static string GetRequestTypeFromBody(string body)
    {
        UpdateConfigRequest? configRequest;

        // Deserializing the body here is necessary in order to check the config type.
        try
        {
            configRequest = JsonConvert.DeserializeObject<UpdateConfigRequest>(body, k_JsonSerializerSettings);
        }
        catch (JsonException ex)
        {
            throw new CliException("Config request body contains invalid JSON. " + ex.Message, null, ExitCode.HandledError);
        }
        catch (Exception ex)
        {
            throw new CliException("Failed to deserialize config request body", ex, ExitCode.UnhandledError);
        }

        if (configRequest is null)
        {
            throw new CliException("Empty config request body", null, ExitCode.HandledError);
        }

        if (string.IsNullOrEmpty(configRequest.Type))
        {
            throw new CliException("Config request body is missing type", null, ExitCode.HandledError);
        }

        return configRequest.Type;
    }

    /// <summary>
    /// Creates an authorization token for the configured service account and attaches it to the http client.
    /// </summary>
    async Task AuthorizeServiceAsync(CancellationToken cancellationToken = default)
    {
        var token = await m_authService.GetAccessTokenAsync(cancellationToken);
        m_httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", token);
    }
}
