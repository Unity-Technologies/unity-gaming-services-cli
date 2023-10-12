using System.Net;
using Unity.Services.Cli.Common.Exceptions;
using Unity.Services.Cli.Common.Models;
using Unity.Services.Cli.Common.Validator;
using Unity.Services.Cli.ServiceAccountAuthentication;
using Unity.Services.Cli.ServiceAccountAuthentication.Token;
using Unity.Services.Gateway.EconomyApiV2.Generated.Api;
using Unity.Services.Gateway.EconomyApiV2.Generated.Client;
using Unity.Services.Gateway.EconomyApiV2.Generated.Model;

namespace Unity.Services.Cli.Economy.Service;

class EconomyService : IEconomyService
{
    readonly IEconomyAdminApiAsync m_EconomyApiAsync;
    readonly IServiceAccountAuthenticationService m_AuthenticationService;
    readonly IConfigurationValidator m_ConfigValidator;

    public EconomyService(IEconomyAdminApiAsync defaultEconomyApiAsync, IConfigurationValidator validator, IServiceAccountAuthenticationService authenticationService)
    {
        m_EconomyApiAsync = defaultEconomyApiAsync;
        m_ConfigValidator = validator;
        m_AuthenticationService = authenticationService;
    }

    internal async Task AuthorizeService(CancellationToken cancellationToken = default)
    {
        var token = await m_AuthenticationService.GetAccessTokenAsync(cancellationToken);
        m_EconomyApiAsync.Configuration.DefaultHeaders.SetAccessTokenHeader(token);
    }

    public async Task<List<GetResourcesResponseResultsInner>> GetResourcesAsync(string projectId, string environmentId,
        CancellationToken cancellationToken = default)
    {
        await AuthorizeService(cancellationToken);
        m_ConfigValidator.ThrowExceptionIfConfigInvalid(Keys.ConfigKeys.ProjectId, projectId);
        m_ConfigValidator.ThrowExceptionIfConfigInvalid(Keys.ConfigKeys.EnvironmentId, environmentId);

        var response = await m_EconomyApiAsync.GetResourcesAsync(projectId, Guid.Parse(environmentId), cancellationToken: cancellationToken);

        if (response == null)
        {
            throw new ApiException(ExitCode.HandledError, "Issue getting remote resources. Note: Maximum value for currencyes is Int32.Max");
        }

        return response.Results;
    }

    public async Task<List<GetResourcesResponseResultsInner>> GetPublishedAsync(string projectId, string environmentId,
        CancellationToken cancellationToken = default)
    {
        await AuthorizeService(cancellationToken);
        m_ConfigValidator.ThrowExceptionIfConfigInvalid(Keys.ConfigKeys.ProjectId, projectId);
        m_ConfigValidator.ThrowExceptionIfConfigInvalid(Keys.ConfigKeys.EnvironmentId, environmentId);

        try
        {
            var response = await m_EconomyApiAsync.GetPublishedResourcesAsync(projectId, Guid.Parse(environmentId),
                cancellationToken: cancellationToken);
            return response.Results;
        }
        catch (ApiException e)
        {
            // If you haven't published before, the service returns a 404 and we can return an empty list.
            if ((HttpStatusCode)e.ErrorCode == HttpStatusCode.NotFound)
            {
                return new List<GetResourcesResponseResultsInner>();
            }

            throw new ApiException(e.ErrorCode, e.Message);
        }
    }

    public async Task PublishAsync(string projectId, string environmentId,
        CancellationToken cancellationToken = default)
    {
        await AuthorizeService(cancellationToken);
        m_ConfigValidator.ThrowExceptionIfConfigInvalid(Keys.ConfigKeys.ProjectId, projectId);
        m_ConfigValidator.ThrowExceptionIfConfigInvalid(Keys.ConfigKeys.EnvironmentId, environmentId);

        await m_EconomyApiAsync.PublishEconomyAsync(projectId, Guid.Parse(environmentId), new PublishBody(true), cancellationToken: cancellationToken);
    }

    public async Task DeleteAsync(string resourceId, string projectId, string environmentId,
        CancellationToken cancellationToken = default)
    {
        await AuthorizeService(cancellationToken);
        m_ConfigValidator.ThrowExceptionIfConfigInvalid(Keys.ConfigKeys.ProjectId, projectId);
        m_ConfigValidator.ThrowExceptionIfConfigInvalid(Keys.ConfigKeys.EnvironmentId, environmentId);

        await m_EconomyApiAsync.DeleteConfigResourceAsync(projectId, Guid.Parse(environmentId), resourceId, cancellationToken: cancellationToken );
    }

    public async Task AddAsync(AddConfigResourceRequest request, string projectId, string environmentId,
        CancellationToken cancellationToken = default)
    {
        await AuthorizeService(cancellationToken);
        m_ConfigValidator.ThrowExceptionIfConfigInvalid(Keys.ConfigKeys.ProjectId, projectId);
        m_ConfigValidator.ThrowExceptionIfConfigInvalid(Keys.ConfigKeys.EnvironmentId, environmentId);

        await m_EconomyApiAsync.AddConfigResourceAsync(projectId, Guid.Parse(environmentId), request, cancellationToken: cancellationToken );
    }

    public async Task EditAsync(string resourceId, AddConfigResourceRequest request, string projectId, string environmentId,
        CancellationToken cancellationToken = default)
    {
        await AuthorizeService(cancellationToken);
        m_ConfigValidator.ThrowExceptionIfConfigInvalid(Keys.ConfigKeys.ProjectId, projectId);
        m_ConfigValidator.ThrowExceptionIfConfigInvalid(Keys.ConfigKeys.EnvironmentId, environmentId);

        await m_EconomyApiAsync.EditConfigResourceAsync(projectId, Guid.Parse(environmentId), resourceId, request, cancellationToken: cancellationToken );
    }
}
