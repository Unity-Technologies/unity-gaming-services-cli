using Unity.Services.Cli.Common.Validator;
using Unity.Services.Cli.ServiceAccountAuthentication;
using Unity.Services.Cli.ServiceAccountAuthentication.Token;
using Unity.Services.Gateway.TriggersApiV1.Generated.Api;
using Unity.Services.Gateway.TriggersApiV1.Generated.Model;

namespace Unity.Services.Cli.Triggers.Service;

class DlqService : IDlqService
{
    readonly IServiceAccountAuthenticationService m_AuthenticationService;
    readonly IDLQApiAsync m_DlqApiAsync;
    readonly IConfigurationValidator m_ConfigValidator;

    public DlqService(
        IDLQApiAsync dlqApiAsync,
        IConfigurationValidator validator,
        IServiceAccountAuthenticationService authenticationService)
    {
        m_DlqApiAsync = dlqApiAsync;
        m_ConfigValidator = validator;
        m_AuthenticationService = authenticationService;
    }

    public async Task<List<DLQEvent>> ListDlqEventsAsync(
        string projectId,
        string environmentId,
        int? limit,
        string? status,
        DateTime? createdFrom,
        DateTime? createdTo,
        string? resolutionAction,
        Guid? eventId,
        CancellationToken cancellationToken = default)
    {
        await AuthorizeServiceAsync(cancellationToken);
        ValidateProjectIdAndEnvironmentId(projectId, environmentId);

        var response = await m_DlqApiAsync.ListDLQEventsAsync(
            Guid.Parse(projectId),
            Guid.Parse(environmentId),
            limit,
            null,
            createdFrom,
            createdTo,
            status,
            resolutionAction,
            eventId,
            cancellationToken: cancellationToken);

        return response.Events;
    }

    public async Task<DLQEvent> GetDlqEventAsync(
        string projectId,
        string environmentId,
        string eventId,
        CancellationToken cancellationToken = default)
    {
        await AuthorizeServiceAsync(cancellationToken);
        ValidateProjectIdAndEnvironmentId(projectId, environmentId);

        return await m_DlqApiAsync.GetDLQEventAsync(
            Guid.Parse(projectId),
            Guid.Parse(environmentId),
            Guid.Parse(eventId),
            cancellationToken: cancellationToken);
    }

    public async Task ReplayDlqEventAsync(
        string projectId,
        string environmentId,
        string eventId,
        CancellationToken cancellationToken = default)
    {
        await AuthorizeServiceAsync(cancellationToken);
        ValidateProjectIdAndEnvironmentId(projectId, environmentId);

        await m_DlqApiAsync.ReplayDLQEventAsync(
            Guid.Parse(projectId),
            Guid.Parse(environmentId),
            Guid.Parse(eventId),
            cancellationToken: cancellationToken);
    }

    public async Task DiscardDlqEventAsync(
        string projectId,
        string environmentId,
        string eventId,
        CancellationToken cancellationToken = default)
    {
        await AuthorizeServiceAsync(cancellationToken);
        ValidateProjectIdAndEnvironmentId(projectId, environmentId);

        await m_DlqApiAsync.DiscardDLQEventAsync(
            Guid.Parse(projectId),
            Guid.Parse(environmentId),
            Guid.Parse(eventId),
            cancellationToken: cancellationToken);
    }

    public async Task<DLQQueuedResult> ReplayAllDlqEventsAsync(
        string projectId,
        string environmentId,
        CancellationToken cancellationToken = default)
    {
        await AuthorizeServiceAsync(cancellationToken);
        ValidateProjectIdAndEnvironmentId(projectId, environmentId);

        return await m_DlqApiAsync.ReplayAllDLQEventsAsync(
            Guid.Parse(projectId),
            Guid.Parse(environmentId),
            cancellationToken: cancellationToken);
    }

    public async Task<DLQDiscardResult> DiscardAllDlqEventsAsync(
        string projectId,
        string environmentId,
        CancellationToken cancellationToken = default)
    {
        await AuthorizeServiceAsync(cancellationToken);
        ValidateProjectIdAndEnvironmentId(projectId, environmentId);

        return await m_DlqApiAsync.DiscardAllDLQEventsAsync(
            Guid.Parse(projectId),
            Guid.Parse(environmentId),
            cancellationToken: cancellationToken);
    }

    async Task AuthorizeServiceAsync(CancellationToken cancellationToken = default)
    {
        var token = await m_AuthenticationService.GetAccessTokenAsync(cancellationToken);
        m_DlqApiAsync.Configuration.DefaultHeaders.SetAccessTokenHeader(token);
    }

    void ValidateProjectIdAndEnvironmentId(string projectId, string environmentId)
    {
        m_ConfigValidator.ThrowExceptionIfConfigInvalid(Common.Models.Keys.ConfigKeys.ProjectId, projectId);
        m_ConfigValidator.ThrowExceptionIfConfigInvalid(Common.Models.Keys.ConfigKeys.EnvironmentId, environmentId);
    }
}
