using System;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Unity.Services.Cli.Common.Exceptions;
using Unity.Services.Cli.Common.Models;
using Unity.Services.Cli.Common.Networking;
using Unity.Services.Cli.IntegrationTest.EnvTests;
using Unity.Services.Cli.MockServer;

namespace Unity.Services.Cli.IntegrationTest.CloudCodeTests;

public class CloudCodeModuleTests : UgsCliFixture
{
    const string k_ValidModuleName = "test_3";
    const string k_ProjectIdNotSetErrorMessage = "'project-id' is not set in project configuration." + " '" + Keys.EnvironmentKeys.ProjectId + "' is not set in system environment variables.";
    const string k_LoggedOutErrorMessage = "You are not logged into any service account."
                                           + " Please login using the 'ugs login' command.";
    const string k_EnvironmentNameNotSetErrorMessage = "'environment-name' is not set in project configuration."
                                                       + " '" + Keys.EnvironmentKeys.EnvironmentName + "' is not set in system environment variables.";

    readonly MockApi m_MockApi = new(NetworkTargetEndpoints.MockServer);

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        m_MockApi.InitServer();
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        m_MockApi.Server?.Dispose();
    }

    [SetUp]
    public async Task SetUp()
    {
        DeleteLocalConfig();
        DeleteLocalCredentials();

        var environmentModels = await IdentityV1MockServerModels.GetModels();
        m_MockApi.Server?.WithMapping(environmentModels.ToArray());

        var cloudCodeModels = await CloudCodeV1MockServerModels.GetModuleModels(k_ValidModuleName);
        m_MockApi.Server?.WithMapping(cloudCodeModels.ToArray());

        CloudCodeV1MockServerModels.OverrideListModules(m_MockApi);
    }

    [TearDown]
    public void TearDown()
    {
        m_MockApi.Server?.ResetMappings();
    }

    // cloud-code module list tests
    [Test]
    public async Task CloudCodeModulesListThrowsProjectIdNotSetException()
    {
        SetConfigValue("environment-name", CommonKeys.ValidEnvironmentName);

        await GetLoggedInCli()
            .Command("cloud-code modules list")
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardOutputContains(k_ProjectIdNotSetErrorMessage)
            .ExecuteAsync();
    }

    [Test]
    public async Task CloudCodeModulesListThrowsNotLoggedInException()
    {
        SetConfigValue("project-id", CommonKeys.ValidProjectId);
        SetConfigValue("environment-name", CommonKeys.ValidEnvironmentName);

        await new UgsCliTestCase()
            .Command("cloud-code modules list")
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardOutputContains(k_LoggedOutErrorMessage)
            .ExecuteAsync();
    }

    [Test]
    public async Task CloudCodeModulesListThrowsEnvironmentIdNotSetException()
    {
        SetConfigValue("project-id", CommonKeys.ValidProjectId);

        await GetLoggedInCli()
            .Command("cloud-code modules list")
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardOutputContains(k_EnvironmentNameNotSetErrorMessage)
            .ExecuteAsync();
    }

    [Test]
    public async Task CloudCodeListReturnsZeroExitCode()
    {
        var expectedMessage = $"ExistingModule{Environment.NewLine}AnotherExistingModule";
        SetConfigValue("project-id", CommonKeys.ValidProjectId);
        SetConfigValue("environment-name", CommonKeys.ValidEnvironmentName);

        await GetLoggedInCli()
            .Command("cloud-code modules list")
            .AssertNoErrors()
            .AssertStandardOutputContains(expectedMessage)
            .ExecuteAsync();
    }

    // cloud-code module delete tests
    [Test]
    public async Task CloudCodeModulesDeleteThrowsProjectIdNotSetException()
    {
        SetConfigValue("environment-name", CommonKeys.ValidEnvironmentName);

        await new UgsCliTestCase()
            .Command($"cloud-code modules delete {k_ValidModuleName}")
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardOutputContains(k_ProjectIdNotSetErrorMessage)
            .ExecuteAsync();
    }

    [Test]
    public async Task CloudCodeModulesDeleteThrowsNotLoggedInException()
    {
        SetConfigValue("project-id", CommonKeys.ValidProjectId);
        SetConfigValue("environment-name", CommonKeys.ValidEnvironmentName);

        await new UgsCliTestCase()
            .Command($"cloud-code modules delete {k_ValidModuleName}")
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardOutputContains(k_LoggedOutErrorMessage)
            .ExecuteAsync();
    }

    [Test]
    public async Task CloudCodeModulesDeleteThrowsInvalidScriptNameException()
    {
        const string invalidModuleName = "test-module";
        const string expectedMsg = $"{invalidModuleName} is not a valid module name. "
            + "A valid module name must only contain letters, numbers and underscores.";
        SetConfigValue("project-id", CommonKeys.ValidProjectId);
        SetConfigValue("environment-name", CommonKeys.ValidEnvironmentName);

        await GetLoggedInCli()
            .Command($"cloud-code modules delete \"{invalidModuleName}\"")
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardOutputContains(expectedMsg)
            .ExecuteAsync();
    }

    [Test]
    public async Task CloudCodeModulesDeleteThrowsEnvironmentIdNotSetException()
    {
        SetConfigValue("project-id", CommonKeys.ValidProjectId);

        await GetLoggedInCli()
            .Command($"cloud-code modules delete {k_ValidModuleName}")
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardOutputContains(k_EnvironmentNameNotSetErrorMessage)
            .ExecuteAsync();
    }


    // cloud code modules get tests
    [TestCase("cloud-code modules get test_module_123")]
    [TestCase("cloud-code modules get test_module_123 -j")]
    public async Task CloudCodeModulesGetThrowsProjectIdNotSetException(string command)
    {
        SetConfigValue("environment-name", CommonKeys.ValidEnvironmentName);

        await new UgsCliTestCase()
            .Command(command)
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardOutputContains(k_ProjectIdNotSetErrorMessage)
            .ExecuteAsync();
    }

    [TestCase("cloud-code modules get test_module_123")]
    [TestCase("cloud-code modules get test_module_123 -j")]
    public async Task CloudCodeModulesGetThrowsNotLoggedInException(string command)
    {
        SetConfigValue("project-id", CommonKeys.ValidProjectId);
        SetConfigValue("environment-name", CommonKeys.ValidEnvironmentName);

        await new UgsCliTestCase()
            .Command(command)
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardOutputContains(k_LoggedOutErrorMessage)
            .ExecuteAsync();
    }

    [TestCase("cloud-code modules get \"test-module-123\"")]
    [TestCase("cloud-code modules get \"test-module-123\" -j")]
    public async Task CloudCodeModulesGetThrowsInvalidModuleNameException(string command)
    {
        const string expectedMsg = "test-module-123 is not a valid module name. "
            + "A valid module name must only contain letters, numbers and underscores.";
        SetConfigValue("project-id", CommonKeys.ValidProjectId);
        SetConfigValue("environment-name", CommonKeys.ValidEnvironmentName);

        await GetLoggedInCli()
            .Command(command)
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardOutputContains(expectedMsg)
            .ExecuteAsync();
    }

    [Test]
    public async Task CloudCodeModulesGetThrowsEnvironmentIdNotSetException()
    {
        SetConfigValue("project-id", CommonKeys.ValidProjectId);

        await GetLoggedInCli()
            .Command("cloud-code modules get test_script_123")
            .AssertStandardOutputContains(k_EnvironmentNameNotSetErrorMessage)
            .AssertExitCode(ExitCode.HandledError)
            .ExecuteAsync();
    }


}
