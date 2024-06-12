using Microsoft.Extensions.Logging;
using Polly;
using Unity.Services.Cli.Common.Policies;
using Unity.Services.Cli.CloudSave.Exceptions;
using Unity.Services.Cli.CloudSave.IO;
using Unity.Services.Gateway.IdentityApiV1.Generated.Client;
using Unity.Services.CloudSave.Authoring.Core.IO;
using Unity.Services.CloudSave.Authoring.Core.Model;
using Unity.Services.CloudSave.Authoring.Core.Service;

namespace Unity.Services.Cli.CloudSave.Deploy;

class CloudSaveClient : ICloudSaveClient
{
    readonly ILogger m_Logger;
    readonly string m_TempPath;
    ICloudSaveSimpleResourceLoader m_Loader;

    public CloudSaveClient(ILogger logger)
    {
        m_Logger = logger;
        //for demo purposes, mock remote storage in a temp dir
        m_TempPath = Path.Join(Path.GetTempPath(), nameof(CloudSaveClient));
        m_Loader = new CloudSaveSimpleResourceLoader(new FileSystem());
    }

    public async Task Initialize(
        string environmentId,
        string projectId,
        CancellationToken cancellationToken)
    {
        // This is sample code, it should be replaced

        //TODO: implement this method
        //For demonstration purposes
        if (!Directory.Exists(m_TempPath))
        {
            var loader = new CloudSaveSimpleResourceLoader(new FileSystem());
            Directory.CreateDirectory(m_TempPath);
            var mockSetup = Enumerable
                .Range(0, 3)
                .Select(CreateResource);
            foreach (var res in mockSetup)
            {
                var item = new SimpleResourceDeploymentItem(res.Name, Path.Combine(m_TempPath, res.Id))
                {
                    Resource = res
                };
                await loader.CreateOrUpdateResource(item, cancellationToken);
            }

            SimpleResource CreateResource(int i)
            {
                return new SimpleResource()
                {
                    Id = $"ID{i}",
                    Name = $"Name {i}",
                    AStrValue = "My Str {i}",
                    NestedObj = new NestedObject
                    {
                        NestedObjectString = i.ToString()
                    }
                };
            }
        }
    }

    public async Task<IResource> Get(string id, CancellationToken cancellationToken)
    {
        //TODO: implement this method
        m_Logger.LogWarning("This is a sample client, it should not be shipped");
        var res = (SimpleResourceDeploymentItem)await m_Loader.ReadResource(Path.Combine(m_TempPath, id), CancellationToken.None);
        return res.Resource;
    }

    public Task Update(IResource resource, CancellationToken cancellationToken)
    {
        //TODO: implement this method
        m_Logger.LogWarning("This is a sample client, it should not be shipped");

        var item = new SimpleResourceDeploymentItem(resource.Id, Path.Combine(m_TempPath, resource.Id))
        {
            Resource = resource
        };
        return m_Loader.CreateOrUpdateResource(item, CancellationToken.None);
    }

    public Task Create(IResource resource, CancellationToken cancellationToken)
    {
        //TODO: implement this method
        m_Logger.LogWarning("This is a sample client, it should not be shipped");
        var item = new SimpleResourceDeploymentItem(resource.Id, Path.Combine(m_TempPath, resource.Id))
        {
            Resource = resource
        };
        return m_Loader.CreateOrUpdateResource(item, CancellationToken.None);
    }

    public Task Delete(IResource resource, CancellationToken cancellationToken)
    {
        //TODO: implement this method
        m_Logger.LogWarning("This is a sample client, it should not be shipped");
        var item = new SimpleResourceDeploymentItem(resource.Id, Path.Combine(m_TempPath, resource.Id))
        {
            Resource = resource
        };
        return m_Loader.DeleteResource(item, CancellationToken.None);
    }

    public async Task<IReadOnlyList<IResource>> List(CancellationToken cancellationToken)
    {
        //TODO: implement this method

        // This is sample code, it should be replaced
        var res = new List<IResource>();
        foreach (var mockFilePath in Directory.EnumerateFiles(m_TempPath))
        {
            var item = (SimpleResourceDeploymentItem)await m_Loader.ReadResource(mockFilePath, CancellationToken.None);
            res.Add(item.Resource);
        }
        return res;
    }

    static readonly HttpClient k_HttpClient = new();

    public async Task<string> RawGetRequest(string? address, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrEmpty(address))
            {
                throw new CloudSaveException($"Invalid Address: {address}");
            }

            // In the case of raw http requests, you can use one of the CLI's retry policies to give the request
            // another chance.
            //
            // Important: For service requests that are managed by the service's Generated Client,
            // do not try to wrap your call in a retry block. You should instead follow the instructions in
            // CloudSaveModule.cs RegisterServices() to set up automatic retries for your service calls.
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

    static T ToPayload<T>(IResource resource) where T : new()
    {
        //TODO: Implement
        return new T();
    }

    static IResource FromPayload<T>(T payload) where T : new()
    {
        //TODO: Implement
        return new SimpleResource();
    }
}
