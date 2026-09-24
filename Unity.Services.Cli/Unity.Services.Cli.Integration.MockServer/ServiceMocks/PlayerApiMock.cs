using Unity.Services.Cli.MockServer.Common;
using Unity.Services.Gateway.PlayerAdminApiV3.Generated.Model;
using WireMock.Admin.Mappings;
using WireMock.Server;

namespace Unity.Services.Cli.MockServer.ServiceMocks;

public class PlayerApiMock : IServiceApiMock
{
    const string k_PlayerAuthConfig = "playerauth-api-v1-generator-config.yaml";
    const string k_AdminConfig = "player-admin-api-v3-generator-config.yaml";
    public const string PlayerId = "player-id";

    public async Task<IReadOnlyList<MappingModel>> CreateMappingModels()
    {
        var playerAuthenticationAdminServiceModels = await MappingModelUtils.ParseMappingModelsFromGeneratorConfigAsync(k_AdminConfig, new());
        playerAuthenticationAdminServiceModels = playerAuthenticationAdminServiceModels.Select(m => m.ConfigMappingPathWithKey(CommonKeys.ProjectIdKey, CommonKeys.ValidProjectId));

        var playerAuthenticationServiceModels = await MappingModelUtils.ParseMappingModelsFromGeneratorConfigAsync(k_PlayerAuthConfig, new());
        playerAuthenticationServiceModels = playerAuthenticationServiceModels.Select(m => m.ConfigMappingPathWithKey(CommonKeys.ProjectIdKey, CommonKeys.ValidProjectId));

        return playerAuthenticationAdminServiceModels.Concat(playerAuthenticationServiceModels).ToArray();
    }

    public void CustomMock(WireMockServer mockServer) { }

    public static PlayerAuthListProjectUserResponse GetPlayerListMock()
    {
        var externalIdResult = new PlayerAuthListProjectUserResponseExternalId()
        {
            ProviderId = "provider-id",
        };

        var externalIdList = new List<PlayerAuthListProjectUserResponseExternalId>();

        externalIdList.Add(externalIdResult);
        externalIdList.Add(externalIdResult);
        externalIdList.Add(externalIdResult);

        var playerResult = new PlayerAuthListProjectUserResponseUser()
        {
            Id = "eyJhbGciOiJIUzI1N",
            Disabled = false,
            ExternalIds = externalIdList,
            CreatedAt = "123000000",
            LastLoginAt = "123000000"
        };

        var players = new List<PlayerAuthListProjectUserResponseUser>();

        players.Add(playerResult);
        players.Add(playerResult);
        players.Add(playerResult);

        var result = new PlayerAuthListProjectUserResponse()
        {
            Next = "xxxxxxxx",
            Results = players
        };

        return result;
    }
}
