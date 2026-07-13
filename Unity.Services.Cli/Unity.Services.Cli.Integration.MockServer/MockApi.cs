using WireMock.Logging;
using WireMock.Server;
using WireMock.Settings;

namespace Unity.Services.Cli.MockServer;

public class MockApi : IDisposable
{
    /// <summary>
    /// Mock server of the API
    /// </summary>
    public WireMockServer Server { get; }

    /// <summary>
    /// The URL the mock server is listening on
    /// </summary>
    public string Url => Server.Url!;

    /// <summary>
    /// Construct MockApi on a random available port
    /// </summary>
    public MockApi()
    {
        Server = WireMockServer.Start(new WireMockServerSettings
        {
            AllowCSharpCodeMatcher = false,
            StartAdminInterface = false,
            ReadStaticMappings = false,
            WatchStaticMappings = false,
            WatchStaticMappingsInSubdirectories = false,
            Logger = new WireMockConsoleLogger(),
            SaveUnmatchedRequests = true
        });

        Console.WriteLine("WireMockServer listening at {0}", Url);
    }

    public async Task MockServiceAsync(IServiceApiMock serviceMock)
    {
        var mappingModels = await serviceMock.CreateMappingModels();
        if (mappingModels.Count > 0)
        {
            Server.WithMapping(mappingModels.ToArray());
        }
        serviceMock.CustomMock(Server!);
    }

    public void Dispose()
    {
        Server.Dispose();
    }
}
