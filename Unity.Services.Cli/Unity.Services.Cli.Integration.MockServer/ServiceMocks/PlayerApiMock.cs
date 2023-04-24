using Unity.Services.Cli.MockServer.Common;
using Unity.Services.Gateway.PlayerAdminApiV3.Generated.Model;
using WireMock.Admin.Mappings;
using WireMock.Server;

namespace Unity.Services.Cli.MockServer.ServiceMocks;

public class PlayerApiMock : IServiceApiMock
{
    const string k_PlayerAuthApiUrl = "https://services.docs.unity.com/specs/v1/706c617965722d61757468.yaml";
    const string k_AdminApiUrl = "https://services.docs.unity.com/specs/v1/706c617965722d617574682d61646d696e.yaml";
    public const string PlayerId = "player-id";

    public async Task<IReadOnlyList<MappingModel>> CreateMappingModels()
    {
        var playerAuthenticationAdminServiceModels = await MappingModelUtils.ParseMappingModelsAsync(k_AdminApiUrl, new());
        playerAuthenticationAdminServiceModels = playerAuthenticationAdminServiceModels.Select(m => m.ConfigMappingPathWithKey(CommonKeys.ProjectIdKey, CommonKeys.ValidProjectId));

        var playerAuthenticationServiceModels = await MappingModelUtils.ParseMappingModelsAsync(k_PlayerAuthApiUrl, new());
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
