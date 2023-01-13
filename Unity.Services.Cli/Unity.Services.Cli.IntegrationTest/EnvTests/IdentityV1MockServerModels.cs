using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.Services.Cli.MockServer;
using WireMock.Admin.Mappings;

namespace Unity.Services.Cli.IntegrationTest.EnvTests;

/// <summary>
/// This is an utility to facilitate any service to add the
/// environment mapped models which are contained in the identity v1 open api specs
/// </summary>
public static class IdentityV1MockServerModels
{
    const string k_IdentityV1OpenApiUrl = "https://services.docs.unity.com/specs/v1/756e697479.yaml";

    public static async Task<IEnumerable<MappingModel>> GetModels()
    {
        var models = await MappingModelUtils.ParseMappingModelsAsync(k_IdentityV1OpenApiUrl, new());
        models = models.Select(m => m.ConfigMappingPathWithKey(CommonKeys.ProjectIdKey, CommonKeys.ValidProjectId)
            .ConfigMappingPathWithKey(CommonKeys.EnvironmentIdKey, CommonKeys.ValidEnvironmentId));

        return models;
    }
}
