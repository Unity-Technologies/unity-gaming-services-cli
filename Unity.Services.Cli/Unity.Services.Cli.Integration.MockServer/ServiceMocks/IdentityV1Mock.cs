using Unity.Services.Cli.MockServer.Common;
using WireMock.Admin.Mappings;
using WireMock.Server;

namespace Unity.Services.Cli.MockServer.ServiceMocks;

public class IdentityV1Mock : IServiceApiMock
{
    const string k_IdentityV1Config = "identity-api-v1-generator-config.yaml";

    public async Task<IReadOnlyList<MappingModel>> CreateMappingModels()
    {
        var models = await MappingModelUtils.ParseMappingModelsFromGeneratorConfigAsync(k_IdentityV1Config, new());
        models = models.Select(m => m.ConfigMappingPathWithKey(CommonKeys.ProjectIdKey, CommonKeys.ValidProjectId)
            .ConfigMappingPathWithKey(CommonKeys.EnvironmentIdKey, CommonKeys.ValidEnvironmentId));

        return models.ToArray();
    }

    public void CustomMock(WireMockServer? mockServer) { }
}
