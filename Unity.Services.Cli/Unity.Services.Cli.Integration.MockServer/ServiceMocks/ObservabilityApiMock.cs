using System.Net;
using Unity.Services.Cli.MockServer.Common;
using Unity.Services.Gateway.ObservabilityApiV1.Generated.Model;
using WireMock.Admin.Mappings;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace Unity.Services.Cli.MockServer.ServiceMocks;

public class ObservabilityApiMock : IServiceApiMock
{
    const string k_ObservabilityPath = "/observability/v1";

    public static readonly LogRecord LogRecord1 = new(
        "2023-09-06T10:02:24.904Z",
        "Error",
        17,
        "Hello, world!",
        new Dictionary<string, object> { { "service.name", "cloud-code" } },
        new Dictionary<string, object> { { "log.type", "script" } });

    public static readonly LogRecord LogRecord2 = new(
        "2023-09-06T10:03:24.904Z",
        "Info",
        9,
        "Goodbye, world!",
        new Dictionary<string, object> { { "service.name", "cloud-code" } },
        new Dictionary<string, object> { { "log.type", "script" } });

    readonly string m_BaseUrl;

    readonly Dictionary<string, string> m_RequestHeader = new()
    {
        { "Content-Type", "application/json" }
    };

    public ObservabilityApiMock()
    {
        m_BaseUrl = $"{k_ObservabilityPath}/projects/{CommonKeys.ValidProjectId}/environments/{CommonKeys.ValidEnvironmentId}/logs";
    }

    public Task<IReadOnlyList<MappingModel>> CreateMappingModels()
    {
        IReadOnlyList<MappingModel> models = new List<MappingModel>();
        return Task.FromResult(models);
    }

    public void CustomMock(WireMockServer mockServer)
    {
        MockGetLogs(mockServer, new List<LogRecord>
        {
            LogRecord1,
            LogRecord2
        });
    }

    void MockGetLogs(WireMockServer mockServer, List<LogRecord> logs, HttpStatusCode code = HttpStatusCode.OK)
    {
        var response = new LogsResponse(0, 100, logs.Count, logs);

        mockServer.Given(Request.Create().WithPath(m_BaseUrl).UsingGet())
            .RespondWith(Response.Create()
                .WithHeaders(m_RequestHeader)
                .WithBodyAsJson(response)
                .WithStatusCode(code));
    }
}
