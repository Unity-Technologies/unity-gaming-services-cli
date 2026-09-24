using WireMock.Admin.Mappings;
using WireMock.Server;

namespace Unity.Services.Cli.MockServer.ServiceMocks;

public class LobbyApiMock : IServiceApiMock
{
    const string k_AuthV1Config = "auth-api-v1-generator-config.yaml";

    public async Task<IReadOnlyList<MappingModel>> CreateMappingModels()
    {
        var authServiceModels = await MappingModelUtils.ParseMappingModelsFromGeneratorConfigAsync(k_AuthV1Config, new());

        var lobbyServiceModels = await MappingModelUtils.ParseMappingModelsFromGeneratorConfigAsync("lobby-api-v1-generator-config.yaml", new());
        return authServiceModels.Concat(lobbyServiceModels).ToArray();
    }

    public void CustomMock(WireMockServer mockServer) { }
}
