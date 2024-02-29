using System;
using System.IO;
using System.Threading.Tasks;
using NUnit.Framework;
using Unity.Services.Cli.MockServer.ServiceMocks;
using Unity.Services.Cli.MockServer.ServiceMocks.GameServerHosting;

namespace Unity.Services.Cli.IntegrationTest.GameServerHostingTests;

public partial class GameServerHostingTests : UgsCliFixture
{
    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        MockApi.Server?.AllowPartialMapping();
        await MockApi.MockServiceAsync(new IdentityV1Mock());
        await MockApi.MockServiceAsync(new GameServerHostingApiMock());
        CreateTempFiles();
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        MockApi.Server?.Dispose();
        MockApi.Server?.ResetMappings();
        DeleteTempFiles();
    }

    [SetUp]
    public void SetUp()
    {
        DeleteLocalConfig();
        DeleteLocalCredentials();
    }

    void CreateTempFiles()
    {
        m_TempDirectoryPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(m_TempDirectoryPath);
        File.WriteAllText(TempFilePath, "file content to upload");
    }

    void DeleteTempFiles()
    {
        if (m_TempDirectoryPath != null && Directory.Exists(m_TempDirectoryPath))
        {
            Directory.Delete(m_TempDirectoryPath, true);
        }
    }

    string? m_TempDirectoryPath;
    string TempFilePath => Path.Combine(m_TempDirectoryPath ?? "", TempFileName);
    static string TempFileName => GameServerHostingApiMock.TempFileName;

    const string k_NotLoggedIn = "You are not logged into any service account.";
    const string k_ProjectIdIsNotSet = "'project-id' is not set in project configuration.";
    const string k_EnvironmentNameIsNotSet = "'environment-name' is not set in project configuration.";

    const string k_BuildConfigurationCreateOrUpdateCommandComplete = "--binary-path simple-game-server-go --build 25289 --command-line \"--init game.init\" --cores 1 --memory 100 --name \"Testing BC\" --query-type sqp --readiness true --speed 100";
    const string k_BuildConfigurationCreateOrUpdateCommandMissingReadiness = "--binary-path simple-game-server-go --build 25289 --command-line \"--init game.init\" --cores 1 --memory 100 --name \"Testing BC\" --query-type sqp --speed 100";
    const string k_BuildConfigurationCreateOrUpdateCommandMissingBinaryPath = "--build 25289 --command-line \"--init game.init\" --cores 1 --memory 100 --name \"Testing BC\" --query-type sqp --speed 100";
    const string k_BuildConfigurationCreateOrUpdateCommandMissingBuild = "--binary-path simple-game-server-go --command-line \"--init game.init\" --cores 1 --memory 100 --name \"Testing BC\" --query-type sqp --speed 100";
    const string k_BuildConfigurationCreateOrUpdateCommandMissingCommandLine = "--binary-path simple-game-server-go --build 25289 --cores 1 --memory 100 --name \"Testing BC\" --query-type sqp --speed 100";
    const string k_BuildConfigurationCreateOrUpdateCommandMissingCores = "--binary-path simple-game-server-go --build 25289 --command-line \"--init game.init\" --memory 100 --name \"Testing BC\" --query-type sqp --speed 100";
    const string k_BuildConfigurationCreateOrUpdateCommandMissingMemory = "--binary-path simple-game-server-go --build 25289 --command-line \"--init game.init\" --cores 1 --name \"Testing BC\" --query-type sqp --speed 100";
    const string k_BuildConfigurationCreateOrUpdateCommandMissingName = "--binary-path simple-game-server-go --build 25289 --command-line \"--init game.init\" --cores 1 --memory 100 --query-type sqp --speed 100";
    const string k_BuildConfigurationCreateOrUpdateCommandMissingQueryType = "--binary-path simple-game-server-go --build 25289 --command-line \"--init game.init\" --cores 1 --memory 100 --name \"Testing BC\" --speed 100";
    const string k_BuildConfigurationCreateOrUpdateCommandMissingSpeed = "--binary-path simple-game-server-go --build 25289 --command-line \"--init game.init\" --cores 1 --memory 100 --name \"Testing BC\" --query-type sqp";
}
