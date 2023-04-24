using Unity.Services.Cli.MockServer.Common;
using WireMock.Admin.Mappings;
using WireMock.Server;

namespace Unity.Services.Cli.MockServer.ServiceMocks;

public class EconomyApiMock : IServiceApiMock
{
    const string k_EconomyOpenApiUrl = "https://services.docs.unity.com/specs/v2/65636f6e6f6d792d61646d696e.yaml";

    public async Task<IReadOnlyList<MappingModel>> CreateMappingModels()
    {
        var economyServiceModels = await MappingModelUtils.ParseMappingModelsAsync(k_EconomyOpenApiUrl, new());
        economyServiceModels = economyServiceModels.Select(
            m => m.ConfigMappingPathWithKey(CommonKeys.ProjectIdKey, CommonKeys.ValidProjectId)
                .ConfigMappingPathWithKey(CommonKeys.EnvironmentIdKey, CommonKeys.ValidEnvironmentId));
        return economyServiceModels.ToArray();
    }

    public void CustomMock(WireMockServer mockServer)
    {
        mockServer.AllowPartialMapping();
    }
}
