using WireMock.Admin.Mappings;
using WireMock.Server;

namespace Unity.Services.Cli.MockServer.ServiceMocks;

public class LobbyApiMock : IServiceApiMock
{
    const string k_AuthV1OpenApiUrl = "https://services.docs.unity.com/specs/v1/61757468.yaml";

    public async Task<IReadOnlyList<MappingModel>> CreateMappingModels()
    {
        var authServiceModels = await MappingModelUtils.ParseMappingModelsAsync(k_AuthV1OpenApiUrl, new());

        var lobbyServiceModels = await MappingModelUtils.ParseMappingModelsFromGeneratorConfigAsync("lobby-api-v1-generator-config.yaml", new());
        return authServiceModels.Concat(lobbyServiceModels).ToArray();
    }

    public void CustomMock(WireMockServer mockServer) { }
}
