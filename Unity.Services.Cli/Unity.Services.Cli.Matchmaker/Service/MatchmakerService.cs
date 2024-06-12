using System.Net;
using Unity.Services.Gateway.MatchmakerAdminApiV3.Generated.Api;
using Unity.Services.Gateway.MatchmakerAdminApiV3.Generated.Model;
using Newtonsoft.Json;
using Unity.Services.Cli.Common.Models;
using Unity.Services.Cli.Common.Validator;
using Unity.Services.Cli.ServiceAccountAuthentication;
using Unity.Services.Cli.ServiceAccountAuthentication.Token;
using Unity.Services.Gateway.MatchmakerAdminApiV3.Generated.Client;
using Unity.Services.Matchmaker.Authoring.Core.Model;
using EnvironmentConfig = Unity.Services.Gateway.MatchmakerAdminApiV3.Generated.Model.EnvironmentConfig;
using QueueConfig = Unity.Services.Gateway.MatchmakerAdminApiV3.Generated.Model.QueueConfig;


namespace Unity.Services.Cli.Matchmaker.Service;

public class MatchmakerService : IMatchmakerService
{
    readonly IMatchmakerAdminApi m_MatchmakerAdminApi;
    readonly IServiceAccountAuthenticationService m_AuthenticationService;
    readonly IConfigurationValidator m_ConfigValidator;
    string m_ProjectId = String.Empty;
    string m_EnvironmentId = String.Empty;

    public MatchmakerService(
        IMatchmakerAdminApi matchmakerAdminApi,
        IServiceAccountAuthenticationService authenticationService,
        IConfigurationValidator validator)
    {
        m_MatchmakerAdminApi = matchmakerAdminApi;
        m_AuthenticationService = authenticationService;
        m_ConfigValidator = validator;
    }

    public async Task<string> Initialize(string projectId, string environmentId, CancellationToken ct = default)
    {
        m_ConfigValidator.ThrowExceptionIfConfigInvalid(Keys.ConfigKeys.ProjectId, projectId);
        m_ConfigValidator.ThrowExceptionIfConfigInvalid(Keys.ConfigKeys.EnvironmentId, environmentId);
        var token = await m_AuthenticationService.GetAccessTokenAsync(ct);
        m_MatchmakerAdminApi.Configuration.DefaultHeaders.SetAccessTokenHeader(token);
        m_ProjectId = projectId;
        m_EnvironmentId = environmentId;

        return string.Empty;
    }

    public async Task<(bool, EnvironmentConfig)> GetEnvironmentConfig(CancellationToken ct = default)
    {
        ApiResponse<EnvironmentConfig>? response;
        try
        {
            response = await m_MatchmakerAdminApi.GetEnvironmentConfigWithHttpInfoAsync(
                m_ProjectId,
                m_EnvironmentId,
                cancellationToken: ct);
        }
        catch (ApiException e)
        {
            if (e.ErrorCode == (int)HttpStatusCode.NotFound)
                return (false, new EnvironmentConfig());
            throw;
        }

        return (true, response.Data);
    }

    public async Task<List<ErrorResponse>> UpsertEnvironmentConfig(
        EnvironmentConfig environmentConfig,
        bool dryRun,
        CancellationToken ct = default)
    {
        try
        {
            await m_MatchmakerAdminApi.UpdateEnvironmentConfigWithHttpInfoAsync(
                m_ProjectId,
                m_EnvironmentId,
                dryRun,
                environmentConfig,
                cancellationToken: ct);
        }
        catch (ApiException e)
        {
            if (e.ErrorCode == (int)HttpStatusCode.BadRequest)
            {
                return JsonConvert.DeserializeObject<ProblemDetails>((string)e.ErrorContent)
                           ?.Details.Select(
                               x => new ErrorResponse()
                               {
                                   ResultCode = x.ResultCode,
                                   Message = x.Message
                               })
                           .ToList()
                       ?? new List<ErrorResponse>();
            }

            throw;
        }

        return new List<ErrorResponse>();
    }

    public async Task<List<QueueConfig>> ListQueues(CancellationToken ct = default)
    {
        ApiResponse<List<QueueConfig>>? response;
        try
        {
            response = await m_MatchmakerAdminApi.ListQueuesWithHttpInfoAsync(
                m_ProjectId,
                m_EnvironmentId,
                cancellationToken: ct);
        }
        catch (ApiException e)
        {
            if (e.ErrorCode == (int)HttpStatusCode.NotFound)
                return new List<QueueConfig>();
            throw;
        }

        return response.Data;
    }

    public async Task<List<ErrorResponse>> UpsertQueueConfig(
        QueueConfig queueConfig,
        bool dryRun,
        CancellationToken ct = default)
    {
        try
        {
            await m_MatchmakerAdminApi.UpsertQueueConfigWithHttpInfoAsync(
                m_ProjectId,
                m_EnvironmentId,
                queueConfig.Name,
                dryRun,
                queueConfig,
                cancellationToken: ct);
        }
        catch (ApiException e)
        {
            if (e.ErrorCode == (int)HttpStatusCode.BadRequest)
            {
                return JsonConvert.DeserializeObject<ProblemDetails>((string)e.ErrorContent)
                           ?.Details.Select(
                               x => new ErrorResponse()
                               {
                                   ResultCode = x.ResultCode,
                                   Message = x.Message
                               })
                           .ToList()
                       ?? new List<ErrorResponse>();
            }

            throw;
        }

        return new List<ErrorResponse>();
    }

    public async Task DeleteQueue(string queueName, bool dryRun, CancellationToken ct = default)
    {
        await m_MatchmakerAdminApi.DeleteQueueAsync(
            m_ProjectId,
            m_EnvironmentId,
            queueName,
            dryRun,
            cancellationToken: ct);
    }
}
