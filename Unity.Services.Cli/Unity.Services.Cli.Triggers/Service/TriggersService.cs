using Polly;
using Unity.Services.Cli.Common.Models;
using Unity.Services.Cli.Common.Policies;
using Unity.Services.Cli.Common.Validator;
using Unity.Services.Cli.ServiceAccountAuthentication;
using Unity.Services.Cli.ServiceAccountAuthentication.Token;
using Unity.Services.Cli.Triggers.Exceptions;
using Unity.Services.Gateway.IdentityApiV1.Generated.Client;
using Unity.Services.Gateway.TriggersApiV1.Generated.Api;
using Unity.Services.Gateway.TriggersApiV1.Generated.Model;

namespace Unity.Services.Cli.Triggers.Service;

class TriggersService : ITriggersService
{
    static readonly HttpClient k_HttpClient = new();

    readonly IServiceAccountAuthenticationService m_AuthenticationService;
    readonly ITriggersApiAsync m_TriggersApiAsync;
    readonly IConfigurationValidator m_ConfigValidator;

    public TriggersService(ITriggersApiAsync triggersApiAsync, IConfigurationValidator validator,
        IServiceAccountAuthenticationService authenticationService)
    {
        m_TriggersApiAsync = triggersApiAsync;
        m_ConfigValidator = validator;
        m_AuthenticationService = authenticationService;
    }

    public async Task<IEnumerable<TriggerConfig>> GetTriggersAsync(string projectId, string environmentId, int? limit, CancellationToken cancellationToken = default)
    {
        await AuthorizeServiceAsync(cancellationToken);
        ValidateProjectIdAndEnvironmentId(projectId, environmentId);

        var response = await m_TriggersApiAsync.ListTriggerConfigsAsync(
            Guid.Parse(projectId),
            Guid.Parse(environmentId),
            limit: limit,
            "",
            cancellationToken: cancellationToken);

        return response.Configs;
    }

    public async Task CreateTriggerAsync(
        string projectId,
        string environmentId,
        TriggerConfigBody config,
        CancellationToken cancellationToken = default)
    {
        await AuthorizeServiceAsync(cancellationToken);
        ValidateProjectIdAndEnvironmentId(projectId, environmentId);

        await m_TriggersApiAsync.CreateTriggerConfigAsync(
            Guid.Parse(projectId),
            Guid.Parse(environmentId),
            config,
            cancellationToken: cancellationToken);
    }

    public async Task UpdateTriggerAsync(
        string projectId,
        string environmentId,
        string triggerId,
        TriggerConfigBody config,
        CancellationToken cancellationToken = default)
    {
        await AuthorizeServiceAsync(cancellationToken);
        ValidateProjectIdAndEnvironmentId(projectId, environmentId);

        await m_TriggersApiAsync.DeleteTriggerConfigAsync(
            Guid.Parse(projectId),
            Guid.Parse(environmentId),
            Guid.Parse(triggerId),
            cancellationToken: cancellationToken);
        await m_TriggersApiAsync.CreateTriggerConfigAsync(
            Guid.Parse(projectId),
            Guid.Parse(environmentId),
            config,
            cancellationToken: cancellationToken);
    }

    public async Task DeleteTriggerAsync(
        string projectId,
        string environmentId,
        string id,
        CancellationToken cancellationToken = default)
    {
        await AuthorizeServiceAsync(cancellationToken);
        ValidateProjectIdAndEnvironmentId(projectId, environmentId);

        await m_TriggersApiAsync.DeleteTriggerConfigAsync(
            Guid.Parse(projectId),
            Guid.Parse(environmentId),
            Guid.Parse(id),
            cancellationToken: cancellationToken);
    }

    public async Task AuthorizeServiceAsync(CancellationToken cancellationToken = default)
    {
        var token = await m_AuthenticationService.GetAccessTokenAsync(cancellationToken);
        m_TriggersApiAsync.Configuration.DefaultHeaders.SetAccessTokenHeader(token);
    }

    public void ValidateProjectIdAndEnvironmentId(string projectId, string environmentId)
    {
        m_ConfigValidator.ThrowExceptionIfConfigInvalid(Keys.ConfigKeys.ProjectId, projectId);
        m_ConfigValidator.ThrowExceptionIfConfigInvalid(Keys.ConfigKeys.EnvironmentId, environmentId);
    }

    public async Task<string> GetRequestAsync(string? address, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrEmpty(address))
            {
                throw new TriggersException($"Invalid Address: {address}");
            }

            // In the case of raw http requests, you can use one of the CLI's retry policies to give the request
            // another chance.
            //
            // Important: For service requests that are managed by the service's Generated Client,
            // do not try to wrap your call in a retry block. You should instead follow the instructions in
            // TriggersModule.cs RegisterServices() to set up automatic retries for your service calls.
            var response = await Policy
                .Handle<IOException>()
                .WaitAndRetryAsync(
                    3,
                    _ => RetryPolicy.GetExponentialBackoffTimeSpan(),
                    (exception, span) => Task.CompletedTask)
                .ExecuteAsync(async () => await k_HttpClient.GetAsync(address, cancellationToken));

            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadAsStringAsync(cancellationToken);
            return result;
        }
        catch (HttpRequestException exception)
        {
            //TODO: define you own service API exception. Here we use `HttpRequestException` as an example to simulate ApiException
            throw new ApiException((int)exception.StatusCode!, exception.Message);
        }
    }

    public async Task WriteToFileAsync(string outputFile, string result, CancellationToken cancellationToken = default)
    {
        await File.WriteAllTextAsync(outputFile, result, cancellationToken);
    }
}
