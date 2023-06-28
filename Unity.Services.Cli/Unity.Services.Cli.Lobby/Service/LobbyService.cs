using Newtonsoft.Json;
using Unity.Services.Cli.Common.Exceptions;
using Unity.Services.Cli.Common.Models;
using Unity.Services.Cli.Common.Networking;
using Unity.Services.Cli.Common.Validator;
using Unity.Services.Cli.ServiceAccountAuthentication;
using Unity.Services.Gateway.Auth.Generated.Api;
using Unity.Services.MpsLobby.LobbyApiV1.Generated.Api;
using Unity.Services.MpsLobby.LobbyApiV1.Generated.Model;
using static Unity.Services.MpsLobby.LobbyApiV1.Generated.Model.TokenRequest;
using IAuthApiAsync = Unity.Services.Gateway.Auth.Generated.Api.IDefaultApiAsync;
using LobbyModel = Unity.Services.MpsLobby.LobbyApiV1.Generated.Model.Lobby;

namespace Unity.Services.Cli.Lobby.Service;

class LobbyService : ILobbyService
{
    /// <summary>
    /// The config validator used to validate project and environment IDs.
    /// </summary>
    readonly IConfigurationValidator m_ConfigValidator;

    /// <summary>
    /// The authentication service used to load the user's token.
    /// </summary>
    readonly IServiceAccountAuthenticationService m_AuthenticationService;

    /// <summary>
    /// The service account token received from the token exchange API.
    /// </summary>
    string? m_serviceToken;

    /// <summary>
    /// The auto-generated Lobby client used for making Lobby API requests.
    /// </summary>
    ILobbyApiAsync? m_LobbyApi;
    ILobbyApiAsync LobbyClient
    {
        get
        {
            // N.B.: Will not be accessed by multiple threads. Make thread-safe if that changes.
            if (m_LobbyApi is null)
            {
                var configuration = new MpsLobby.LobbyApiV1.Generated.Client.Configuration();
                configuration.AccessToken = m_serviceToken!;
                configuration.BasePath = EndpointHelper.GetCurrentEndpointFor<LobbyApiEndpoints>();
                configuration.DefaultHeaders.SetXClientIdHeader();
                m_LobbyApi = new LobbyApi(configuration);
            }

            return m_LobbyApi;
        }
    }

    /// <summary>
    /// The auto-generated Auth client used for making requests to the token-exchange endpoint.
    /// </summary>
    IAuthApiAsync? m_AuthApi;
    IAuthApiAsync AuthClient
    {
        get
        {
            // N.B.: Will not be accessed by multiple threads. Make thread-safe if that changes.
            return m_AuthApi ??= new DefaultApi(EndpointHelper.GetCurrentEndpointFor<UnityServicesGatewayEndpoints>());
        }
    }

    /// <summary>
    /// The Lobby service used by the CLI.
    /// </summary>
    public LobbyService(IConfigurationValidator validator, IServiceAccountAuthenticationService authenticationService, ILobbyApiAsync? lobbyClient, IAuthApiAsync? authApi)
    {
        m_ConfigValidator = validator;
        m_AuthenticationService = authenticationService;
        m_LobbyApi = lobbyClient;
        m_AuthApi = authApi;
    }

    // <inheritdoc />
    public async Task<LobbyModel> BulkUpdateLobbyAsync(string? projectId, string? environmentId, string? serviceId, string lobbyId, string body, CancellationToken cancellationToken)
    {
        ValidateRequestIds(projectId, environmentId, serviceId);
        await SetServiceAccountToken(projectId!, environmentId!, cancellationToken);
        var bulkUpdateRequest = DeserializeOrThrow<BulkUpdateRequest>(body);
        return await LobbyClient.BulkUpdateLobbyAsync(
            string.Empty,
            string.Empty,
            lobbyId,
            serviceId: serviceId,
            bulkUpdateRequest: bulkUpdateRequest,
            cancellationToken: cancellationToken);
    }

    // <inheritdoc />
    public async Task<LobbyModel> CreateLobbyAsync(string? projectId, string? environmentId, string? serviceId, string? impersonatedPlayerId, string body, CancellationToken cancellationToken)
    {
        ValidateRequestIds(projectId, environmentId, serviceId);
        await SetServiceAccountToken(projectId!, environmentId!, cancellationToken);
        var createRequest = DeserializeOrThrow<CreateRequest>(body);
        return await LobbyClient.CreateLobbyAsync(
            string.Empty,
            string.Empty,
            serviceId: serviceId,
            impersonatedUserId: impersonatedPlayerId,
            createRequest: createRequest,
            cancellationToken: cancellationToken);
    }

    // <inheritdoc />
    public async Task DeleteLobbyAsync(string? projectId, string? environmentId, string? serviceId, string? impersonatedPlayerId, string lobbyId, CancellationToken cancellationToken)
    {
        ValidateRequestIds(projectId, environmentId, serviceId);
        await SetServiceAccountToken(projectId!, environmentId!, cancellationToken);
        await LobbyClient.DeleteLobbyAsync(
            string.Empty,
            string.Empty,
            lobbyId,
            serviceId: serviceId,
            impersonatedUserId: impersonatedPlayerId,
            cancellationToken: cancellationToken);
    }

    // <inheritdoc />
    public async Task<IEnumerable<string>> GetJoinedLobbiesAsync(string? projectId, string? environmentId, string? serviceId, string? impersonatedPlayerId, CancellationToken cancellationToken)
    {
        ValidateRequestIds(projectId, environmentId, serviceId);
        if (string.IsNullOrEmpty(impersonatedPlayerId))
        {
            throw new CliException("Player ID must be provided for the GetJoinedLobbies API.", ExitCode.HandledError);
        }

        await SetServiceAccountToken(projectId!, environmentId!, cancellationToken);
        return await LobbyClient.GetJoinedLobbiesAsync(
            string.Empty,
            string.Empty,
            serviceId: serviceId,
            impersonatedUserId: impersonatedPlayerId,
            cancellationToken: cancellationToken);
    }

    // <inheritdoc />
    public async Task<IEnumerable<string>> GetHostedLobbiesAsync(string? projectId, string? environmentId, string? serviceId, string? impersonatedPlayerId, CancellationToken cancellationToken)
    {
        ValidateRequestIds(projectId, environmentId, serviceId);
        await SetServiceAccountToken(projectId!, environmentId!, cancellationToken);
        return await LobbyClient.GetHostedLobbiesAsync(
            string.Empty,
            string.Empty,
            serviceId: serviceId,
            impersonatedUserId: impersonatedPlayerId,
            cancellationToken: cancellationToken);
    }

    // <inheritdoc />
    public async Task<LobbyModel> GetLobbyAsync(string? projectId, string? environmentId, string? serviceId, string? impersonatedPlayerId, string lobbyId, CancellationToken cancellationToken)
    {
        ValidateRequestIds(projectId, environmentId, serviceId);
        await SetServiceAccountToken(projectId!, environmentId!, cancellationToken);
        return await LobbyClient.GetLobbyAsync(
            string.Empty,
            string.Empty,
            lobbyId,
            serviceId: serviceId,
            impersonatedUserId: impersonatedPlayerId,
            cancellationToken: cancellationToken);
    }

    // <inheritdoc />
    public async Task<LobbyModel> JoinLobbyAsync(string? projectId, string? environmentId, string? serviceId, string? lobbyId, string? lobbyCode, string player, CancellationToken cancellationToken)
    {
        ValidateRequestIds(projectId, environmentId, serviceId);
        await SetServiceAccountToken(projectId!, environmentId!, cancellationToken);
        var playerObject = DeserializeOrThrow<Player>(player);

        // Join by ID.
        if (!string.IsNullOrEmpty(lobbyId))
        {
            return await LobbyClient.JoinLobbyByIdAsync(
                string.Empty,
                string.Empty,
                lobbyId,
                serviceId: serviceId,
                impersonatedUserId: playerObject!.Id,
                joinLobbyByIdRequest: new JoinLobbyByIdRequest(playerObject),
                cancellationToken: cancellationToken);
        }

        // Join by code.
        if (!string.IsNullOrEmpty(lobbyCode))
        {
            var joinByCodeRequest = new JoinByCodeRequest(lobbyCode: lobbyCode, player: playerObject!);
            return await LobbyClient.JoinLobbyByCodeAsync(
                string.Empty,
                string.Empty,
                joinByCodeRequest: joinByCodeRequest,
                serviceId: serviceId,
                impersonatedUserId: playerObject!.Id,
                cancellationToken: cancellationToken);
        }

        throw new CliException("Either lobby ID or lobby code must be provided to join a lobby.", ExitCode.HandledError);
    }

    // <inheritdoc />
    public async Task<LobbyModel> ReconnectAsync(string? projectId, string? environmentId, string? serviceId, string? impersonatedPlayerId, string lobbyId, CancellationToken cancellationToken)
    {
        ValidateRequestIds(projectId, environmentId, serviceId);
        if (string.IsNullOrEmpty(impersonatedPlayerId))
        {
            throw new CliException("Player ID must be provided for the Reconnect API.", ExitCode.HandledError);
        }

        await SetServiceAccountToken(projectId!, environmentId!, cancellationToken);
        return await LobbyClient.ReconnectAsync(
            string.Empty,
            string.Empty,
            lobbyId,
            serviceId: serviceId,
            impersonatedUserId: impersonatedPlayerId,
            cancellationToken: cancellationToken);
    }

    // <inheritdoc />
    public async Task<QueryResponse> QueryLobbiesAsync(string? projectId, string? environmentId, string? serviceId, string? impersonatedPlayerId, string? body, CancellationToken cancellationToken)
    {
        ValidateRequestIds(projectId, environmentId, serviceId);
        await SetServiceAccountToken(projectId!, environmentId!, cancellationToken);
        var queryRequest = !string.IsNullOrEmpty(body) ? DeserializeOrThrow<QueryRequest>(body) : new QueryRequest();
        return await LobbyClient.QueryLobbiesAsync(
            string.Empty,
            string.Empty,
            queryRequest: queryRequest,
            serviceId: serviceId,
            impersonatedUserId: impersonatedPlayerId,
            cancellationToken: cancellationToken);
    }

    // <inheritdoc />
    public async Task<LobbyModel> QuickJoinAsync(string? projectId, string? environmentId, string? serviceId, string filter, string player, CancellationToken cancellationToken)
    {
        ValidateRequestIds(projectId, environmentId, serviceId);
        await SetServiceAccountToken(projectId!, environmentId!, cancellationToken);
        var filterObject = DeserializeOrThrow<List<QueryFilter>>(filter);
        var playerObject = DeserializeOrThrow<Player>(player);
        var quickJoinRequest = new QuickJoinRequest(filter: filterObject!, player: playerObject!);
        return await LobbyClient.QuickJoinLobbyAsync(
            string.Empty,
            string.Empty,
            quickJoinRequest: quickJoinRequest,
            serviceId: serviceId,
            impersonatedUserId: playerObject!.Id,
            cancellationToken: cancellationToken);
    }

    // <inheritdoc />
    public async Task RemovePlayerAsync(string? projectId, string? environmentId, string? serviceId, string? playerId, string lobbyId, CancellationToken cancellationToken)
    {
        ValidateRequestIds(projectId, environmentId, serviceId);
        if (string.IsNullOrEmpty(playerId))
        {
            throw new CliException("Player ID must be provided for the Remove Player API.", ExitCode.HandledError);
        }

        await SetServiceAccountToken(projectId!, environmentId!, cancellationToken);
        await LobbyClient.RemovePlayerAsync(
            string.Empty,
            string.Empty,
            lobbyId,
            playerId!,
            serviceId: serviceId,
            impersonatedUserId: playerId,
            cancellationToken: cancellationToken);
    }

    // <inheritdoc />
    public async Task<LobbyModel> UpdatePlayerAsync(string? projectId, string? environmentId, string? serviceId, string? playerId, string lobbyId, string body, CancellationToken cancellationToken)
    {
        ValidateRequestIds(projectId, environmentId, serviceId);
        if (string.IsNullOrEmpty(playerId))
        {
            throw new CliException("Player ID must be provided for the Update Player API.", ExitCode.HandledError);
        }

        await SetServiceAccountToken(projectId!, environmentId!, cancellationToken);
        var playerUpdateRequest = DeserializeOrThrow<PlayerUpdateRequest>(body);
        return await LobbyClient.UpdatePlayerAsync(
            string.Empty,
            string.Empty,
            lobbyId,
            playerId!,
            playerUpdateRequest: playerUpdateRequest,
            serviceId: serviceId,
            impersonatedUserId: playerId,
            cancellationToken: cancellationToken);
    }

    // <inheritdoc />
    public async Task<LobbyModel> UpdateLobbyAsync(string? projectId, string? environmentId, string? serviceId, string? impersonatedPlayerId, string lobbyId, string body, CancellationToken cancellationToken)
    {
        ValidateRequestIds(projectId, environmentId, serviceId);
        await SetServiceAccountToken(projectId!, environmentId!, cancellationToken);
        var updateRequest = DeserializeOrThrow<UpdateRequest>(body);
        return await LobbyClient.UpdateLobbyAsync(
            string.Empty,
            string.Empty,
            lobbyId,
            updateRequest: updateRequest,
            serviceId: serviceId,
            impersonatedUserId: impersonatedPlayerId,
            cancellationToken: cancellationToken);
    }

    // <inheritdoc />
    public async Task HeartbeatLobbyAsync(string? projectId, string? environmentId, string? serviceId, string? impersonatedPlayerId, string lobbyId, CancellationToken cancellationToken)
    {
        ValidateRequestIds(projectId, environmentId, serviceId);
        await SetServiceAccountToken(projectId!, environmentId!, cancellationToken);
        await LobbyClient.HeartbeatAsync(
            string.Empty,
            string.Empty,
            lobbyId,
            serviceId: serviceId,
            impersonatedUserId: impersonatedPlayerId,
            cancellationToken: cancellationToken);
    }

    // <inheritdoc />
    public async Task<IDictionary<string, TokenData>> RequestTokenAsync(string? projectId, string? environmentId, string? serviceId, string? impersonatedPlayerId, string lobbyId, TokenTypeEnum tokenType, CancellationToken cancellationToken)
    {
        ValidateRequestIds(projectId, environmentId, serviceId);
        if (string.IsNullOrEmpty(impersonatedPlayerId))
        {
            throw new CliException("Player ID must be provided for the Request Token API.", ExitCode.HandledError);
        }

        await SetServiceAccountToken(projectId!, environmentId!, cancellationToken);
        var tokenRequest = new List<TokenRequest> { new TokenRequest(tokenType) };
        return await LobbyClient.RequestTokensAsync(
            string.Empty,
            string.Empty,
            lobbyId,
            tokenRequest: tokenRequest,
            serviceId: serviceId,
            impersonatedUserId: impersonatedPlayerId,
            cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Helper function to validate the ID strings required for Lobby API requests.
    /// It will throw if any of the strings are null or empty.
    /// </summary>
    /// <param name="projectId">The project ID being used for the Lobby request.</param>
    /// <param name="environmentId">The environment ID being used for the Lobby request.</param>
    /// <param name="serviceId">The service ID being used for the Lobby request.</param>
    void ValidateRequestIds(string? projectId, string? environmentId, string? serviceId)
    {
        m_ConfigValidator.ThrowExceptionIfConfigInvalid(Keys.ConfigKeys.ProjectId, projectId ?? "");
        m_ConfigValidator.ThrowExceptionIfConfigInvalid(Keys.ConfigKeys.EnvironmentId, environmentId ?? "");
        if (string.IsNullOrEmpty(serviceId))
        {
            throw new CliException("A non-empty service ID must be provided.", ExitCode.HandledError);
        }
    }

    /// <summary>
    /// Helper function to use the /auth/v1/token-exchange endpoint in order to get and set the Lobby client's service token.
    /// </summary>
    /// <param name="projectId">The project ID to use in the token exchange.</param>
    /// <param name="environmentId">The environment ID to use in the token exchange.</param>
    /// <param name="cancellationToken">Token used to cancel the task.</param>
    async Task SetServiceAccountToken(string projectId, string environmentId, CancellationToken cancellationToken)
    {
        if (AuthClient.Configuration is null)
        {
            throw new CliException("Authentication client is missing configuration.", ExitCode.UnhandledError);
        }

        var token = await GetToken(cancellationToken);
        AuthClient.Configuration.DefaultHeaders["Authorization"] = $"Basic {token}";
        var response = await AuthClient.ExchangeToStatelessAsync(projectId, environmentId, cancellationToken: cancellationToken);
        var accessToken = response?.AccessToken;
        if (string.IsNullOrEmpty(accessToken))
        {
            throw new CliException("Unable to convert service token into JWT.", ExitCode.UnhandledError);
        }

        m_serviceToken = accessToken;
    }

    /// <summary>
    /// Helper function to get the user's current authentication token.
    /// </summary>
    /// <param name="cancellationToken">Token used to cancel the task.</param>
    async Task<string> GetToken(CancellationToken cancellationToken)
    {
        try
        {
            return await m_AuthenticationService.GetAccessTokenAsync(cancellationToken);
        }
        catch (Exception e)
        {
            throw new CliException($"Error loading authentication token.", e, ExitCode.UnhandledError);
        }
    }

    /// <summary>
    /// Helper function to wrap JSON deserialization in order to throw a <see cref="CliException"/> for errors.
    /// </summary>
    static T? DeserializeOrThrow<T>(string value)
    {
        try
        {
            return JsonConvert.DeserializeObject<T>(value);
        }
        catch (Exception ex)
        {
            throw new CliException("Failed to deserialize object for Lobby request.", ex, ExitCode.HandledError);
        }
    }
}
