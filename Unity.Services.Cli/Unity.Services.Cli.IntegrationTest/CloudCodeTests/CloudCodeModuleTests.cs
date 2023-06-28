using System;
using System.IO;
using System.Threading.Tasks;
using NUnit.Framework;
using Unity.Services.Cli.Common.Exceptions;
using Unity.Services.Cli.Common.Models;
using Unity.Services.Cli.IntegrationTest.Common;
using Unity.Services.Cli.MockServer.Common;
using Unity.Services.Cli.MockServer.ServiceMocks;
using Unity.Services.Cli.MockServer.ServiceMocks.CloudCode;

namespace Unity.Services.Cli.IntegrationTest.CloudCodeTests;

[Ignore("Temporarily ignoring, will fix in separate PR")]
public class CloudCodeModuleTests : UgsCliFixture
{
    static readonly string k_TestDirectory = Path.Combine(UgsCliBuilder.RootDirectory, ".tmp/FilesDir");
    const string k_ImportTestFileDirectory
        = "Unity.Services.Cli/Unity.Services.Cli.CloudCode.UnitTest/ModuleTestCases";
    const string k_ValidModuleName = "test_3";
    const string k_ProjectIdNotSetErrorMessage = "'project-id' is not set in project configuration." + " '" + Keys.EnvironmentKeys.ProjectId + "' is not set in system environment variables.";
    const string k_LoggedOutErrorMessage = "You are not logged into any service account."
                                           + " Please login using the 'ugs login' command.";
    const string k_EnvironmentNameNotSetErrorMessage = "'environment-name' is not set in project configuration."
                                                       + " '" + Keys.EnvironmentKeys.EnvironmentName + "' is not set in system environment variables.";
    const string k_NotValidModuleNameMessage = "is not a valid module name. A valid module name must only contain letters, numbers and underscores.";

    [SetUp]
    public async Task SetUp()
    {
        DeleteLocalConfig();
        DeleteLocalCredentials();

        await m_MockApi.MockServiceAsync(new IdentityV1Mock());
        await m_MockApi.MockServiceAsync(new CloudCodeV1Mock());
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
            .AssertStandardErrorContains(k_ProjectIdNotSetErrorMessage)
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
            .AssertStandardErrorContains(k_LoggedOutErrorMessage)
            .ExecuteAsync();
    }

    [Test]
    public async Task CloudCodeModulesListThrowsEnvironmentIdNotSetException()
    {
        SetConfigValue("project-id", CommonKeys.ValidProjectId);

        await GetLoggedInCli()
            .Command("cloud-code modules list")
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardErrorContains(k_EnvironmentNameNotSetErrorMessage)
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
            .AssertStandardErrorContains(k_ProjectIdNotSetErrorMessage)
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
            .AssertStandardErrorContains(k_LoggedOutErrorMessage)
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
            .AssertStandardErrorContains(expectedMsg)
            .ExecuteAsync();
    }

    [Test]
    public async Task CloudCodeModulesDeleteThrowsEnvironmentIdNotSetException()
    {
        SetConfigValue("project-id", CommonKeys.ValidProjectId);

        await GetLoggedInCli()
            .Command($"cloud-code modules delete {k_ValidModuleName}")
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardErrorContains(k_EnvironmentNameNotSetErrorMessage)
            .ExecuteAsync();
    }


    // cloud code modules get tests
    [TestCase(
        "cloud-code modules get test_module_123",
        "",
        k_ProjectIdNotSetErrorMessage)
    ]
    [TestCase(
        "cloud-code modules get test_module_123 -j",
        "",
        k_ProjectIdNotSetErrorMessage)
    ]
    public async Task CloudCodeModulesGetThrowsProjectIdNotSetException(
        string command,
        string expectedStandardOutput,
        string expectedErrorOutput)
    {
        SetConfigValue("environment-name", CommonKeys.ValidEnvironmentName);

        await new UgsCliTestCase()
            .Command(command)
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardOutputContains(expectedStandardOutput)
            .AssertStandardErrorContains(expectedErrorOutput)
            .ExecuteAsync();
    }

    [TestCase(
        "cloud-code modules get test_module_123",
        "",
        k_LoggedOutErrorMessage)
    ]
    [TestCase(
        "cloud-code modules get test_module_123 -j",
        "",
        k_LoggedOutErrorMessage)
    ]
    public async Task CloudCodeModulesGetThrowsNotLoggedInException(
        string command,
        string expectedStandardOutput,
        string expectedErrorOutput)
    {
        SetConfigValue("project-id", CommonKeys.ValidProjectId);
        SetConfigValue("environment-name", CommonKeys.ValidEnvironmentName);

        await new UgsCliTestCase()
            .Command(command)
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardOutputContains(expectedStandardOutput)
            .AssertStandardErrorContains(expectedErrorOutput)
            .ExecuteAsync();
    }

    [TestCase(
        "cloud-code modules get \"test-module-123\"",
        "",
        k_NotValidModuleNameMessage)
    ]
    [TestCase(
        "cloud-code modules get \"test-module-123\" -j",
        "",
        k_NotValidModuleNameMessage)
    ]
    public async Task CloudCodeModulesGetThrowsInvalidModuleNameException(
        string command,
        string expectedStandardOutput,
        string expectedErrorOutput)
    {
        SetConfigValue("project-id", CommonKeys.ValidProjectId);
        SetConfigValue("environment-name", CommonKeys.ValidEnvironmentName);

        await GetLoggedInCli()
            .Command(command)
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardOutputContains(expectedStandardOutput)
            .AssertStandardErrorContains(expectedErrorOutput)
            .ExecuteAsync();
    }

    [Test]
    public async Task CloudCodeModulesGetThrowsEnvironmentIdNotSetException()
    {
        SetConfigValue("project-id", CommonKeys.ValidProjectId);

        await GetLoggedInCli()
            .Command("cloud-code modules get test_script_123")
            .AssertStandardErrorContains(k_EnvironmentNameNotSetErrorMessage)
            .AssertExitCode(ExitCode.HandledError)
            .ExecuteAsync();
    }

    [Test]
    [Ignore("Temporarily ignored, will make a new PR to fix")]
    public async Task CloudCodeModulesImport_Success()
    {
        const string expectedMsg = $"Module [test_3] successfully created";

        SetConfigValue("project-id", CommonKeys.ValidProjectId);
        SetConfigValue("environment-name", CommonKeys.ValidEnvironmentName);

        await GetLoggedInCli()
            .Command($"cloud-code modules import {k_ImportTestFileDirectory} test.ccmzip")
            .AssertStandardOutputContains(expectedMsg)
            .ExecuteAsync();
    }

    [Test]
    [Ignore("Temporarily ignored, will make a new PR to fix")]
    public async Task CloudCodeModulesImport_NoFilenameSpecified_Error()
    {
        const string expectedMsg = $"The file at 'Unity.Services.Cli/Unity.Services.Cli.CloudCode.UnitTest/ModuleTestCases\\ugs.ccmzip' could not be found. Ensure the file exists and the specified path is correct";

        SetConfigValue("project-id", CommonKeys.ValidProjectId);
        SetConfigValue("environment-name", CommonKeys.ValidEnvironmentName);

        await GetLoggedInCli()
            .Command($"cloud-code modules import {k_ImportTestFileDirectory}")
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardOutputContains(expectedMsg)
            .ExecuteAsync();
    }

    [Test]
    [Ignore("Temporarily ignored, will make a new PR to fix")]
    public async Task CloudCodeModulesExport_Success()
    {
        const string filename = "test.ccmzip";
        const string expectedMsg = $"Exporting your environment...";
        var filePath = Path.Join(k_TestDirectory, filename);

        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }

        SetConfigValue("project-id", CommonKeys.ValidProjectId);
        SetConfigValue("environment-name", CommonKeys.ValidEnvironmentName);

        await GetLoggedInCli()
            .Command($"cloud-code modules export {k_TestDirectory} {filename}")
            .AssertNoErrors()
            .AssertStandardOutputContains(expectedMsg)
            .ExecuteAsync();

        Assert.IsTrue(File.Exists(Path.Join(k_TestDirectory, filename)));
    }

    [Test]
    [Ignore("Temporarily ignored, will make a new PR to fix")]
    public async Task CloudCodeModulesExport_NoFilenameSpecified_Success()
    {
        const string filename = "ugs.ccmzip";
        const string expectedMsg = $"Exporting your environment...";
        var filePath = Path.Join(k_TestDirectory, filename);

        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }
        SetConfigValue("project-id", CommonKeys.ValidProjectId);
        SetConfigValue("environment-name", CommonKeys.ValidEnvironmentName);

        await GetLoggedInCli()
            .Command($"cloud-code modules export {k_TestDirectory}")
            .AssertNoErrors()
            .AssertStandardOutputContains(expectedMsg)
            .ExecuteAsync();

        Assert.IsTrue(File.Exists(filePath));
    }

    [Test]
    [Ignore("Temporarily ignored, will make a new PR to fix")]
    public async Task CloudCodeModulesExport_FileAlreadyExists_Error()
    {
        const string filename = "test.ccmzip";
        const string expectedMsg = $"The filename to export to already exists. Please create a new file";
        var filePath = Path.Join(k_TestDirectory, filename);

        if (!File.Exists(filePath))
        {
            File.Create(filePath);
        }

        SetConfigValue("project-id", CommonKeys.ValidProjectId);
        SetConfigValue("environment-name", CommonKeys.ValidEnvironmentName);

        await GetLoggedInCli()
            .Command($"cloud-code modules export {k_TestDirectory} {filename}")
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardOutputContains(expectedMsg)
            .ExecuteAsync();
    }

}
