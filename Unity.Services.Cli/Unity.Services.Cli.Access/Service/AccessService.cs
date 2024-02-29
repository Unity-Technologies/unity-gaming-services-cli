using Newtonsoft.Json;
using Unity.Services.Cli.Common.Exceptions;
using Unity.Services.Cli.ServiceAccountAuthentication;
using Unity.Services.Cli.ServiceAccountAuthentication.Token;
using Unity.Services.Gateway.AccessApiV1.Generated.Api;
using Unity.Services.Gateway.AccessApiV1.Generated.Client;
using Unity.Services.Gateway.AccessApiV1.Generated.Model;

namespace Unity.Services.Cli.Access.Service;

class AccessService : IAccessService
{
    readonly IPlayerPolicyApi m_PlayerPolicyApi;
    readonly IProjectPolicyApi m_ProjectPolicyApi;
    readonly IServiceAccountAuthenticationService m_AuthenticationService;

    public AccessService(IProjectPolicyApi projectPolicyApi, IPlayerPolicyApi playerPolicyApi,
        IServiceAccountAuthenticationService authenticationService)
    {
        m_ProjectPolicyApi = projectPolicyApi;
        m_PlayerPolicyApi = playerPolicyApi;
        m_AuthenticationService = authenticationService;
    }

    const string k_JsonIncorrectFormatExceptionMessage = "Please make sure that the format of your JSON input is correct and all required fields are included. If you need help, please refer to the documentation.";

    static string ReadFile(FileInfo file)
    {
        if (!file.Exists)
        {
            throw new CliException($"The file does not exist at the provided path: {file.DirectoryName}", ExitCode.HandledError);
        }

        using var sr = file.OpenText();
        var jsonString = sr.ReadToEnd().Trim('\r', '\n');

        return jsonString;
    }

    internal async Task AuthorizeServiceAsync(CancellationToken cancellationToken = default)
    {
        var token = await m_AuthenticationService.GetAccessTokenAsync(cancellationToken);
        m_ProjectPolicyApi.Configuration.DefaultHeaders.SetAccessTokenHeader(token);
        m_PlayerPolicyApi.Configuration.DefaultHeaders.SetAccessTokenHeader(token);
    }

    public async Task<Policy> GetPolicyAsync(string projectId, string environmentId,
        CancellationToken cancellationToken = default)
    {
        await AuthorizeServiceAsync(cancellationToken);
        try
        {
            var response =
                await m_ProjectPolicyApi.GetPolicyAsync(projectId, environmentId, cancellationToken: cancellationToken);
            return response;
        }
        catch (ApiException e)
        {
            throw new CliException(e.Message, ExitCode.HandledError);
        }
    }

    public async Task<PlayerPolicy> GetPlayerPolicyAsync(string projectId, string environmentId, string playerId,
        CancellationToken cancellationToken = default)
    {
        await AuthorizeServiceAsync(cancellationToken);

        try
        {
            var response =
                await m_PlayerPolicyApi.GetPlayerPolicyAsync(projectId, environmentId, playerId, cancellationToken: cancellationToken);
            return response;
        }
        catch (ApiException e)
        {
            throw new CliException(e.Message, ExitCode.HandledError);
        }
    }

    public async Task<List<PlayerPolicy>> GetAllPlayerPoliciesAsync(string projectId, string environmentId,
        CancellationToken cancellationToken = default)
    {
        await AuthorizeServiceAsync(cancellationToken);

        try
        {
            var response =
                await m_PlayerPolicyApi.GetAllPlayerPoliciesAsync(
                    projectId,
                    environmentId,
                    100,
                    cancellationToken: cancellationToken);
            List<PlayerPolicy> results = response!.Results;

            while (response.Next != null)
            {
                response = await m_PlayerPolicyApi.GetAllPlayerPoliciesAsync(
                    projectId,
                    environmentId,
                    next: response.Next,
                    cancellationToken: cancellationToken);
                results = results.Concat(response.Results).ToList();
            }

            return results;
        }
        catch (ApiException e)
        {
            throw new CliException(e.Message, ExitCode.HandledError);
        }
    }

    public async Task UpsertPolicyAsync(string projectId, string environmentId, FileInfo file,
        CancellationToken cancellationToken = default)
    {
        await AuthorizeServiceAsync(cancellationToken);

        var jsonString = ReadFile(file);

        Policy? policy;
        try
        {
            policy = JsonConvert.DeserializeObject<Policy>(jsonString, new JsonSerializerSettings
            {
                MissingMemberHandling = MissingMemberHandling.Error,
            });
        }
        catch
        {
            throw new CliException(k_JsonIncorrectFormatExceptionMessage, ExitCode.HandledError);
        }

        try
        {
            await m_ProjectPolicyApi.UpsertPolicyAsync(projectId, environmentId, policy, cancellationToken: cancellationToken);
        }
        catch (ApiException e)
        {
            throw new CliException(e.Message, ExitCode.HandledError);
        }
    }

    public async Task UpsertPlayerPolicyAsync(string projectId, string environmentId, string playerId, FileInfo file,
        CancellationToken cancellationToken = default)
    {
        await AuthorizeServiceAsync(cancellationToken);

        var jsonString = ReadFile(file);

        Policy? policy;
        try
        {
            policy = JsonConvert.DeserializeObject<Policy>(jsonString, new JsonSerializerSettings
            {
                MissingMemberHandling = MissingMemberHandling.Error,
            });
        }
        catch
        {
            throw new CliException(k_JsonIncorrectFormatExceptionMessage, ExitCode.HandledError);
        }

        try
        {
            await m_PlayerPolicyApi.UpsertPlayerPolicyAsync(projectId, environmentId, playerId, policy, cancellationToken: cancellationToken);
        }
        catch (ApiException e)
        {
            throw new CliException(e.Message, ExitCode.HandledError);
        }
    }

    public async Task DeletePolicyStatementsAsync(string projectId, string environmentId, FileInfo file,
        CancellationToken cancellationToken = default)
    {
        await AuthorizeServiceAsync(cancellationToken);

        var jsonString = ReadFile(file);

        DeleteOptions? deleteOptions;
        try
        {
            deleteOptions = JsonConvert.DeserializeObject<DeleteOptions>(jsonString, new JsonSerializerSettings
            {
                MissingMemberHandling = MissingMemberHandling.Error,
            });
        }
        catch
        {
            throw new CliException(k_JsonIncorrectFormatExceptionMessage, ExitCode.HandledError);
        }

        try
        {
            await m_ProjectPolicyApi.DeletePolicyStatementsAsync(projectId, environmentId, deleteOptions, cancellationToken: cancellationToken);
        }
        catch (ApiException e)
        {
            throw new CliException(e.Message, ExitCode.HandledError);
        }
    }

    public async Task DeletePlayerPolicyStatementsAsync(string projectId, string environmentId, string playerId, FileInfo file,
        CancellationToken cancellationToken = default)
    {
        await AuthorizeServiceAsync(cancellationToken);

        var jsonString = ReadFile(file);

        DeleteOptions? deleteOptions;
        try
        {
            deleteOptions = JsonConvert.DeserializeObject<DeleteOptions>(jsonString, new JsonSerializerSettings
            {
                MissingMemberHandling = MissingMemberHandling.Error,
            });
        }
        catch
        {
            throw new CliException(k_JsonIncorrectFormatExceptionMessage, ExitCode.HandledError);
        }

        try
        {
            await m_PlayerPolicyApi.DeletePlayerPolicyStatementsAsync(projectId, environmentId, playerId, deleteOptions, cancellationToken: cancellationToken);
        }
        catch (ApiException e)
        {
            throw new CliException(e.Message, ExitCode.HandledError);
        }
    }

    public async Task UpsertProjectAccessCaCAsync(
        string projectId,
        string environmentId,
        Policy policy,
        CancellationToken cancellationToken = default)
    {
        await AuthorizeServiceAsync(cancellationToken);

        try
        {
            await m_ProjectPolicyApi.UpsertPolicyAsync(projectId, environmentId, policy, cancellationToken: cancellationToken);
        }
        catch (ApiException e)
        {
            throw new CliException(e.Message, ExitCode.HandledError);
        }
    }

    public async Task DeleteProjectAccessCaCAsync(
        string projectId,
        string environmentId,
        DeleteOptions options,
        CancellationToken cancellationToken = default)
    {
        await AuthorizeServiceAsync(cancellationToken);
        try
        {
            await m_ProjectPolicyApi.DeletePolicyStatementsAsync(projectId, environmentId, options, cancellationToken: cancellationToken);
        }
        catch (ApiException e)
        {
            throw new CliException(e.Message, ExitCode.HandledError);
        }
    }
}
