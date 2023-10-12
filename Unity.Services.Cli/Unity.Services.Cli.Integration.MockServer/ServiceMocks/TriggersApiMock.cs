using System.Net;
using Unity.Services.Cli.MockServer.Common;
using Unity.Services.Gateway.TriggersApiV1.Generated.Model;
using WireMock.Admin.Mappings;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace Unity.Services.Cli.MockServer.ServiceMocks;

public class TriggersApiMock : IServiceApiMock
{
    const string k_TriggersPath = "/triggers/v0";
    const string k_BaseUrl = $"{k_TriggersPath}/projects/{CommonKeys.ValidProjectId}/environments/{CommonKeys.ValidEnvironmentId}/configs";

    public static readonly TriggerConfig Trigger1 = new(
        Guid.Parse("00000000-0000-0000-0000-000000000001"), DateTime.Now, DateTime.Now, "Trigger1",
        Guid.Parse(CommonKeys.ValidProjectId), Guid.Parse(CommonKeys.ValidEnvironmentId), "eventType",
        TriggerActionType.CloudCode, "cloudcode:blah"
        );
    public static readonly TriggerConfig Trigger2 = new(
        Guid.Parse("00000000-0000-0000-0000-000000000002"), DateTime.Now, DateTime.Now, "Trigger2",
        Guid.Parse(CommonKeys.ValidProjectId), Guid.Parse(CommonKeys.ValidEnvironmentId), "eventType",
        TriggerActionType.CloudCode, "cloudcode:blah"
    );


    readonly Dictionary<string, string> m_RequestHeader = new ()
    {
        { "Content-Type", "application/json" }
    };

    public Task<IReadOnlyList<MappingModel>> CreateMappingModels()
    {
        IReadOnlyList<MappingModel> models = new List<MappingModel>();
        return Task.FromResult(models);
    }

    public void CustomMock(WireMockServer mockServer)
    {
        MockListTriggers(mockServer, new List<TriggerConfig>()
        {
            Trigger1,
            Trigger2
        });

        MockGetTrigger(mockServer, Trigger1);
        MockDeleteTrigger(mockServer, Trigger1.Id.ToString());
        MockCreateTrigger(mockServer, Trigger1);
    }

    void MockListTriggers(WireMockServer mockServer, List<TriggerConfig> triggerConfigs,
        HttpStatusCode code = HttpStatusCode.OK)
    {
        var response = new TriggerConfigPage()
        {
            Configs = triggerConfigs
        };

        mockServer.Given(Request.Create().WithPath(k_BaseUrl).UsingGet())
            .RespondWith(Response.Create()
                .WithHeaders(m_RequestHeader)
                .WithBodyAsJson(response)
                .WithStatusCode(code));
    }

    void MockGetTrigger(WireMockServer mockServer, TriggerConfig triggerConfig, HttpStatusCode code = HttpStatusCode.OK)
    {
        mockServer.Given(Request.Create().WithPath(k_BaseUrl + "/" + triggerConfig.Id).UsingGet())
            .RespondWith(Response.Create()
                .WithHeaders(m_RequestHeader)
                .WithBodyAsJson(triggerConfig)
                .WithStatusCode(code));
    }

    void MockCreateTrigger(WireMockServer mockServer, TriggerConfig triggerConfig, HttpStatusCode code = HttpStatusCode.Created)
    {
        mockServer.Given(Request.Create().WithPath(k_BaseUrl).UsingPost())
            .RespondWith(Response.Create()
                .WithHeaders(m_RequestHeader)
                .WithBodyAsJson(triggerConfig)
                .WithStatusCode(code));
    }

    void MockDeleteTrigger(WireMockServer mockServer, string triggerId, HttpStatusCode code = HttpStatusCode.NoContent)
    {
        mockServer.Given(Request.Create().WithPath(k_BaseUrl + "/" + triggerId).UsingDelete())
            .RespondWith(Response.Create()
                .WithHeaders(m_RequestHeader)
                .WithStatusCode(code));
    }
}
