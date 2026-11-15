using System.Net;
using Newtonsoft.Json;
using Unity.Services.Cli.Common.Exceptions;
using Unity.Services.Cli.Common.Models;
using Unity.Services.Cli.Common.Validator;
using Unity.Services.Cli.Player.IdProvider.Models;
using Unity.Services.Cli.ServiceAccountAuthentication;
using Unity.Services.Cli.ServiceAccountAuthentication.Token;
using Unity.Services.Gateway.PlayerIdentityApiV2.Generated.Api;
using Unity.Services.Gateway.PlayerIdentityApiV2.Generated.Client;
using Unity.Services.Gateway.PlayerIdentityApiV2.Generated.Model;

namespace Unity.Services.Cli.Player.IdProvider.Service;

class IdProviderService : IIdProviderService
{
    const string k_PermissionPrefix = "player_auth.id_provider.";

    readonly IAuthenticationProvidersApiAsync m_Api;
    readonly IServiceAccountAuthenticationService m_AuthService;
    readonly IConfigurationValidator m_ConfigValidator;

    public IdProviderService(
        IAuthenticationProvidersApiAsync api,
        IServiceAccountAuthenticationService authService,
        IConfigurationValidator configValidator)
    {
        m_Api = api;
        m_Api.ExceptionFactory = RaiseForUnusableResponses;
        m_AuthService = authService;
        m_ConfigValidator = configValidator;
    }

    // The generated client's default factory raises only for HTTP 400 and above. A request that got no response
    // (DNS, TLS, timeout), or a 2xx that is not the API's JSON (a proxy or network sign-in page), otherwise returns
    // null, and a delete or state change would report success.
    internal static Exception? RaiseForUnusableResponses(string methodName, IApiResponse response)
    {
        var status = (int)response.StatusCode;
        if (status == 0)
        {
            return new ApiException(0, $"Error calling {methodName}: {response.ErrorText}", response.ErrorText ?? "");
        }

        if (status is >= 200 and < 300)
        {
            var reason = UnreadableReason(response);
            return reason is null ? null : new ApiException(status, $"Error calling {methodName}: {reason}", reason);
        }

        return Unity.Services.Gateway.PlayerIdentityApiV2.Generated.Client.Configuration.DefaultExceptionFactory(methodName, response);
    }

    static string? UnreadableReason(IApiResponse response)
    {
        if (!string.IsNullOrEmpty(response.ErrorText))
        {
            return response.ErrorText;
        }

        if (string.IsNullOrWhiteSpace(response.RawContent))
        {
            return null;
        }

        var header = response.Headers?
            .FirstOrDefault(entry => string.Equals(entry.Key, "Content-Type", StringComparison.OrdinalIgnoreCase));
        var contentType = header?.Value is { } values ? string.Join(", ", values) : "";
        return contentType.Contains("json", StringComparison.OrdinalIgnoreCase)
            ? null
            : $"the response was {(contentType.Length == 0 ? "not marked as JSON" : contentType)}, not JSON";
    }

    public async Task<IReadOnlyList<IdProviderResponse>> ListAsync(string projectId, string? type = null, CancellationToken cancellationToken = default)
    {
        var project = await AuthorizeAsync(projectId, cancellationToken);
        var response = await CallAsync(
            () => m_Api.IdProviderListAsync(project, type!, cancellationToken: cancellationToken),
            "list the identity providers", "list", null);
        return response.Results?.Select(ToResponse).ToList() ?? new List<IdProviderResponse>();
    }

    public async Task<IdProviderResponse> GetAsync(string projectId, string configId, CancellationToken cancellationToken = default)
    {
        var project = await AuthorizeAsync(projectId, cancellationToken);
        var response = await CallAsync(
            () => m_Api.IdProviderConfigGetAsync(project, ParseConfigId(configId), cancellationToken: cancellationToken),
            $"get configuration {configId}", "get", null);
        return ToResponse(response);
    }

    public async Task<IdProviderResponse> CreateAsync(string projectId, CreateIdProviderRequest request, CancellationToken cancellationToken = default)
    {
        var project = await AuthorizeAsync(projectId, cancellationToken);
        var body = new CreateIDProviderV2Request(
            clientId: request.ClientId!,
            clientSecret: request.ClientSecret!,
            displayName: request.DisplayName!,
            relyingParty: request.RelyingParty!,
            oidcConfig: ToOidcConfig(request.OidcConfig)!,
            steamConfig: ToSteamConfig(request.SteamConfig)!,
            type: request.Type);
        var response = await CallAsync(
            () => m_Api.IdProviderCreateAsync(project, body, cancellationToken: cancellationToken),
            $"add a '{request.Type}' configuration", "create", request.ClientSecret);
        return ToResponse(response);
    }

    public async Task<IdProviderResponse> UpdateAsync(string projectId, string configId, UpdateIdProviderRequest request, CancellationToken cancellationToken = default)
    {
        var project = await AuthorizeAsync(projectId, cancellationToken);
        var body = new IDProviderV2Properties(
            clientId: request.ClientId!,
            clientSecret: request.ClientSecret!,
            displayName: request.DisplayName!,
            relyingParty: request.RelyingParty!,
            oidcConfig: ToOidcConfig(request.OidcConfig)!,
            steamConfig: ToSteamConfig(request.SteamConfig)!);
        var response = await CallAsync(
            () => m_Api.IdProviderConfigUpdateAsync(project, ParseConfigId(configId), body, cancellationToken: cancellationToken),
            $"update configuration {configId}", "update", request.ClientSecret);
        return ToResponse(response);
    }

    public async Task<IdProviderResponse> EnableAsync(string projectId, string configId, CancellationToken cancellationToken = default)
    {
        var project = await AuthorizeAsync(projectId, cancellationToken);
        var response = await CallAsync(
            () => m_Api.IdProviderEnableAsync(project, ParseConfigId(configId), cancellationToken: cancellationToken),
            $"enable configuration {configId}", "enable", null);
        return ToResponse(response);
    }

    public async Task<IdProviderResponse> DisableAsync(string projectId, string configId, CancellationToken cancellationToken = default)
    {
        var project = await AuthorizeAsync(projectId, cancellationToken);
        var response = await CallAsync(
            () => m_Api.IdProviderDisableAsync(project, ParseConfigId(configId), cancellationToken: cancellationToken),
            $"disable configuration {configId}", "disable", null);
        return ToResponse(response);
    }

    public async Task DeleteAsync(string projectId, string configId, CancellationToken cancellationToken = default)
    {
        var project = await AuthorizeAsync(projectId, cancellationToken);
        await CallAsync(
            async () =>
            {
                await m_Api.IdProviderDeleteConfigAsync(project, ParseConfigId(configId), cancellationToken: cancellationToken);
                return true;
            },
            $"delete configuration {configId}", "delete", null);
    }

    async Task<Guid> AuthorizeAsync(string projectId, CancellationToken cancellationToken)
    {
        m_ConfigValidator.ThrowExceptionIfConfigInvalid(Keys.ConfigKeys.ProjectId, projectId);
        var token = await m_AuthService.GetAccessTokenAsync(cancellationToken);
        m_Api.Configuration.DefaultHeaders.SetAccessTokenHeader(token);
        return Guid.Parse(projectId);
    }

    static Guid ParseConfigId(string configId)
    {
        if (!Guid.TryParse(configId, out var id))
        {
            throw new CliException($"'{configId}' is not a configuration ID.", ExitCode.HandledError);
        }

        return id;
    }

    static async Task<T> CallAsync<T>(Func<Task<T>> call, string action, string permission, string? sentSecret)
    {
        T result;
        try
        {
            result = await call();
        }
        catch (ApiException exception)
        {
            throw Failure((HttpStatusCode)exception.ErrorCode, exception.ErrorContent as string ?? "", action,
                k_PermissionPrefix + permission, sentSecret, changesState: permission is not ("list" or "get"));
        }

        return result ?? throw new CliException(
            $"The services gateway returned no data when trying to {action}.", ExitCode.HandledError);
    }

    // A problem's detail can echo the request body, so the secret that body carried is masked out of it.
    internal static CliException Failure(HttpStatusCode status, string body, string action, string permission, string? sentSecret,
        bool changesState)
    {
        var summary = ProblemSummary(body, sentSecret);
        var message = status switch
        {
            0 => $"Could not reach the services gateway to {action}: {Mask(body, sentSecret)}",
            >= HttpStatusCode.OK and < HttpStatusCode.Ambiguous =>
                $"Could not read the answer when trying to {action}: {Mask(body, sentSecret).TrimEnd('.', ' ')}. A proxy "
                + "or network sign-in page may be answering instead of the services gateway."
                + (changesState ? " Run `ugs player id-provider list` to check whether the change was applied." : ""),
            HttpStatusCode.Unauthorized =>
                $"The services gateway rejected the credentials when trying to {action}.{summary} Log in again with "
                + "`ugs login`, or as a service account with `ugs login --service-key-id <key-id> --secret-key-stdin`.",
            HttpStatusCode.Forbidden =>
                $"Missing permission to {action}.{summary} The account needs `{permission}` on this project.",
            HttpStatusCode.NotFound => $"Not found when trying to {action}.{summary}",
            HttpStatusCode.Conflict => $"Could not {action}: it conflicts with an existing configuration.{summary}",
            HttpStatusCode.TooManyRequests => $"Rate limited when trying to {action}; retry later.{summary}",
            _ => $"Failed to {action} (HTTP {(int)status}).{summary}",
        };
        return new CliException(message, ExitCode.HandledError);
    }

    static string ProblemSummary(string body, string? sentSecret)
    {
        ProblemResponse? problem;
        try
        {
            problem = JsonConvert.DeserializeObject<ProblemResponse>(body);
        }
        catch (JsonException)
        {
            return "";
        }

        var request = string.IsNullOrWhiteSpace(problem?.RequestId) ? "" : $" (request ID {problem!.RequestId})";
        if (string.IsNullOrWhiteSpace(problem?.Title))
        {
            return request.Length == 0 ? "" : $" Request ID {problem!.RequestId}.";
        }

        var title = Mask(problem!.Title!, sentSecret);
        if (problem.Code is not null)
        {
            title = $"{title} (code {problem.Code})";
        }

        var detail = string.IsNullOrWhiteSpace(problem.Detail) ? null : Mask(problem.Detail, sentSecret);
        return detail is null ? $" {title}{request}." : $" {title}: {detail}{request}.";
    }

    // A problem that quotes the request body carries the secret JSON-escaped, so both forms are masked.
    static string Mask(string text, string? secret)
    {
        if (string.IsNullOrEmpty(secret))
        {
            return text;
        }

        var escapedSecret = JsonConvert.ToString(secret)[1..^1];
        return text
            .Replace(secret, "***", StringComparison.Ordinal)
            .Replace(escapedSecret, "***", StringComparison.Ordinal);
    }

    static IdProviderResponse ToResponse(IDProviderV2 provider) => new()
    {
        ConfigId = provider.ConfigId.ToString(),
        Type = provider.Type,
        ClientId = provider.ClientId,
        DisplayName = provider.DisplayName,
        RelyingParty = provider.RelyingParty,
        Disabled = provider.Disabled,
        HasClientSecret = provider.HasClientSecret,
        OidcConfig = provider.OidcConfig is null ? null : new OpenIdConfig { Issuer = provider.OidcConfig.Issuer },
        SteamConfig = provider.SteamConfig is null
            ? null
            : new SteamProviderConfig
            {
                AdditionalAppIds = provider.SteamConfig.AdditionalAppIds?
                    .Select(app => new AdditionalAppId { AppId = app.AppId, Description = app.Description })
                    .ToList(),
            },
    };

    static OpenIDConnectConfig? ToOidcConfig(OpenIdConfig? config) =>
        config is null ? null : new OpenIDConnectConfig(config.Issuer!);

    static SteamConfig? ToSteamConfig(SteamProviderConfig? config) =>
        config is null
            ? null
            : new SteamConfig((config.AdditionalAppIds ?? new List<AdditionalAppId>())
                .Select(app => new SteamAppConfig(app.AppId ?? "", app.Description!))
                .ToList());
}
