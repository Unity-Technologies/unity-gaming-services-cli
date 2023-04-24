using WireMock.Logging;
using WireMock.Server;
using WireMock.Settings;

namespace Unity.Services.Cli.MockServer;

public class MockApi : IDisposable
{
    /// <summary>
    /// Mock server of the API
    /// </summary>
    public WireMockServer? Server { get; }


    string m_ServiceUrl;

    /// <summary>
    /// Construct MockApi with mock url
    /// </summary>
    /// <param name="url">Local url that the mock server is listening to</param>
    public MockApi(string url)
    {
        m_ServiceUrl = url;
        Server = WireMockServer.Start(new WireMockServerSettings
        {
            AllowCSharpCodeMatcher = true,
            Urls = new[] { m_ServiceUrl },
            StartAdminInterface = true,
            ReadStaticMappings = true,
            WatchStaticMappings = true,
            WatchStaticMappingsInSubdirectories = true,
            Logger = new WireMockConsoleLogger(),
            SaveUnmatchedRequests = true
        });

        Console.WriteLine("WireMockServer listening at {0}", m_ServiceUrl);
    }

    public async Task MockServiceAsync(IServiceApiMock serviceMock)
    {
        var mappingModels = await serviceMock.CreateMappingModels();
        if (mappingModels.Count > 0)
        {
            Server!.WithMapping(mappingModels.ToArray());
        }
        serviceMock.CustomMock(Server!);
    }

    public void Dispose()
    {
        Server?.Dispose();
    }
}
