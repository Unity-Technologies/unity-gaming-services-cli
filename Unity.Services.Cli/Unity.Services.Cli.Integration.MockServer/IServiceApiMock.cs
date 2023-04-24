using WireMock.Admin.Mappings;
using WireMock.Server;

namespace Unity.Services.Cli.MockServer;

public interface IServiceApiMock
{
    /// <summary>
    /// Create mapping modules for a service
    /// </summary>
    /// <returns>models to be used by MockApi</returns>
    public Task<IReadOnlyList<MappingModel>> CreateMappingModels();


    /// <summary>
    /// custom behaviour to mock service api for mock server
    /// </summary>
    /// <param name="mockServer"></param>
    void CustomMock(WireMockServer mockServer);
}
