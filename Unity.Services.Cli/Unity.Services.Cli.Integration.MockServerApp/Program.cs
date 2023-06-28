using Unity.Services.Cli.Common.Networking;
using Unity.Services.Cli.MockServer;
using Unity.Services.Cli.MockServer.Common;
using Unity.Services.Cli.MockServer.ServiceMocks;
using Unity.Services.Cli.MockServer.ServiceMocks.CloudCode;

namespace Unity.Services.Cli.Integration.MockServerApp;

static class Program
{
    static async Task Main()
    {
        using var mockApi = new MockApi(NetworkTargetEndpoints.MockServer);
        using var integrationConfig = new IntegrationConfig();
        try
        {
            await MockIntegrationTestAsync(mockApi, integrationConfig);
            Console.ReadLine();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
    }

    static async Task MockIntegrationTestAsync(MockApi mockApi, IntegrationConfig integrationConfig)
    {
        // An example of testing valid configuration, change this example to your expected configuration
        integrationConfig.SetConfigValue("project-id", CommonKeys.ValidProjectId);
        integrationConfig.SetConfigValue("environment-name", CommonKeys.ValidEnvironmentName);
        integrationConfig.SetCredentialValue(CommonKeys.ValidAccessToken);

        //Replace these mock with your service mocks
        await mockApi.MockServiceAsync(new IdentityV1Mock());
        await mockApi.MockServiceAsync(new CloudCodeV1Mock());
    }
}
