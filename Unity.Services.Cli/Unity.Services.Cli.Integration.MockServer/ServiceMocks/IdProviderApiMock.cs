using System.Net;
using Unity.Services.Cli.MockServer.Common;
using WireMock.Admin.Mappings;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace Unity.Services.Cli.MockServer.ServiceMocks;

/// <summary>
/// The services gateway's player-identity v2 identity provider routes, with a Steam and a Username and
/// Password configuration on the project.
/// </summary>
public class IdProviderApiMock : IServiceApiMock
{
    public const string SteamConfigId = "8bdacc33-6eef-4577-beb0-633c86259f5b";
    public const string PasswordConfigId = "77777777-0000-4000-8000-000000000007";
    public const string CreatedConfigId = "41fe0c2e-0000-4000-8000-00000000000a";

    static readonly string k_Idps = $"/api/player-identity/v2/projects/{CommonKeys.ValidProjectId}/idps";

    static readonly object k_Steam = new
    {
        configId = SteamConfigId,
        type = "steampowered.com",
        clientId = "480",
        disabled = false,
        hasClientSecret = true,
        steamConfig = new { additionalAppIds = new[] { new { appId = "481", description = "Demo" } } },
    };

    static readonly object k_UsernamePassword = new
    {
        configId = PasswordConfigId,
        type = "usernamepassword",
        clientId = "",
        disabled = false,
    };

    public Task<IReadOnlyList<MappingModel>> CreateMappingModels()
    {
        return Task.FromResult<IReadOnlyList<MappingModel>>(Array.Empty<MappingModel>());
    }

    public void CustomMock(WireMockServer mockServer)
    {
        Map(mockServer, Request.Create().WithPath(k_Idps).UsingGet(),
            new { total = 2, results = new[] { k_Steam, k_UsernamePassword } });
        Map(mockServer, Request.Create().WithPath(k_Idps).UsingPost(),
            new { configId = CreatedConfigId, type = "google.com", clientId = "client", disabled = false });
        Map(mockServer, Request.Create().WithPath($"{k_Idps}/{SteamConfigId}").UsingGet(), k_Steam);
        Map(mockServer, Request.Create().WithPath($"{k_Idps}/{SteamConfigId}").UsingPatch(),
            new { configId = SteamConfigId, type = "steampowered.com", clientId = "999", disabled = false });
        Map(mockServer, Request.Create().WithPath($"{k_Idps}/{SteamConfigId}/disable").UsingPost(),
            new { configId = SteamConfigId, type = "steampowered.com", clientId = "480", disabled = true });
        Map(mockServer, Request.Create().WithPath($"{k_Idps}/{SteamConfigId}/enable").UsingPost(),
            new { configId = SteamConfigId, type = "steampowered.com", clientId = "480", disabled = false });
        Map(mockServer, Request.Create().WithPath($"{k_Idps}/{SteamConfigId}").UsingDelete(), new { });
    }

    static void Map(WireMockServer mockServer, IRequestBuilder request, object body)
    {
        mockServer.Given(request)
            .RespondWith(Response.Create()
                .WithHeaders(new Dictionary<string, string> { { "Content-Type", "application/json" } })
                .WithBodyAsJson(body)
                .WithStatusCode(HttpStatusCode.OK));
    }
}
