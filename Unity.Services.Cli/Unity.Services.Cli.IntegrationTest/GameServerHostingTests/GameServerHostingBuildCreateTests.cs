using System.Threading.Tasks;
using NUnit.Framework;
using Unity.Services.Cli.Common.Exceptions;
using Unity.Services.Cli.IntegrationTest.Common;
using Unity.Services.Cli.MockServer.Common;

namespace Unity.Services.Cli.IntegrationTest.GameServerHostingTests;

public partial class GameServerHostingTests
{
    const string k_BuildCreateCommand = "gsh build create --name test-build --os-family linux --type CONTAINER";

    [Test]
    [Category("gsh")]
    [Category("gsh build")]
    [Category("gsh build create")]
    public async Task BuildCreate_SucceedsWithValidInput()
    {
        await GetFullySetCli()
            .Command(k_BuildCreateCommand)
            .AssertStandardOutput(
                v =>
                {
                    StringAssert.Contains("Creating build...", v);
                    StringAssert.Contains("buildId: ", v);
                })
            .AssertNoErrors()
            .ExecuteAsync();
    }

    [Test]
    [Category("gsh")]
    [Category("gsh build")]
    [Category("gsh build create")]
    public async Task BuildCreate_FailsMissingNameException()
    {
        await GetFullySetCli()
            .Command("gsh build create --os-family linux --type CONTAINER")
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardErrorContains("Option '--name' is required.")
            .ExecuteAsync();
    }

    [Test]
    [Category("gsh")]
    [Category("gsh build")]
    [Category("gsh build create")]
    public async Task BuildCreate_ThrowsMissingOsFamilyException()
    {
        await GetFullySetCli()
            .Command("gsh build create --name test-build --type CONTAINER")
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardErrorContains("Option '--os-family' is required.")
            .ExecuteAsync();
    }

    [Test]
    [Category("gsh")]
    [Category("gsh build")]
    [Category("gsh build create")]
    public async Task BuildCreate_ThrowsInvalidTypeException()
    {
        await GetFullySetCli()
            .Command("gsh build create --name test-build --os-family linux --type VM")
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardErrorContains(
                "Invalid option for --type. Did you mean one of the following? FILEUPLOAD, CONTAINER, S3, GCS")
            .ExecuteAsync();
    }

    [Test]
    [Category("gsh")]
    [Category("gsh build")]
    [Category("gsh build create")]
    public async Task BuildCreate_ThrowsMissingTypeException()
    {
        await GetFullySetCli()
            .Command("gsh build create --name test-build --os-family linux")
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardErrorContains("Option '--type' is required.")
            .ExecuteAsync();
    }

    [Test]
    [Category("gsh")]
    [Category("gsh build")]
    [Category("gsh build create")]
    public async Task BuildCreate_ThrowsInvalidOsFamilyException()
    {
        await GetFullySetCli()
            .Command("gsh build create --name test-build --os-family solaris --type CONTAINER")
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardErrorContains("Invalid option for --os-family. Did you mean one of the following? LINUX")
            .ExecuteAsync();
    }

    [Test]
    [Category("gsh")]
    [Category("gsh build")]
    [Category("gsh build create")]
    public async Task BuildCreate_ThrowsNotLoggedInException()
    {
        SetConfigValue("project-id", CommonKeys.ValidProjectId);
        SetConfigValue("environment-id", CommonKeys.ValidEnvironmentId);
        SetConfigValue("environment-name", CommonKeys.ValidEnvironmentName);

        await new UgsCliTestCase()
            .Command(k_BuildCreateCommand)
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardErrorContains(k_NotLoggedIn)
            .ExecuteAsync();
    }

    [Test]
    [Category("gsh")]
    [Category("gsh build")]
    [Category("gsh build create")]
    public async Task BuildCreate_ThrowsProjectIdNotSetException()
    {
        SetConfigValue("environment-id", CommonKeys.ValidEnvironmentId);
        SetConfigValue("environment-name", CommonKeys.ValidEnvironmentName);
        await GetLoggedInCli()
            .Command(k_BuildCreateCommand)
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardErrorContains(k_ProjectIdIsNotSet)
            .ExecuteAsync();
    }

    [Test]
    [Category("gsh")]
    [Category("gsh build")]
    [Category("gsh build create")]
    public async Task BuildCreate_ThrowsEnvironmentIdNotSetException()
    {
        SetConfigValue("project-id", CommonKeys.ValidProjectId);
        await GetLoggedInCli()
            .Command(k_BuildCreateCommand)
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardErrorContains(k_EnvironmentNameIsNotSet)
            .ExecuteAsync();
    }
}
