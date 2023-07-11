using System.Threading.Tasks;
using NUnit.Framework;
using Unity.Services.Cli.Common.Exceptions;
using Unity.Services.Cli.IntegrationTest.Common;
using Unity.Services.Cli.MockServer.Common;

namespace Unity.Services.Cli.IntegrationTest.GameServerHostingTests;

public partial class GameServerHostingTests
{
    const string k_BuildDeleteCommand = "gsh build delete 1";

    [Test]
    [Category("gsh")]
    [Category("gsh build")]
    [Category("gsh build delete")]
    public async Task BuildDelete_SucceedsWithValidInput()
    {
        await GetFullySetCli()
            .Command(k_BuildDeleteCommand)
            .AssertExitCode(ExitCode.Success)
            .AssertStandardOutputContains("Deleting build...")
            .AssertStandardErrorContains("Build deleted successfully")
            .ExecuteAsync();
    }

    [Test]
    [Category("gsh")]
    [Category("gsh build")]
    [Category("gsh build delete")]
    public async Task BuildDelete_ThrowsMissingBuildIdException()
    {
        await GetFullySetCli()
            .Command("gsh build delete")
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardErrorContains("Required argument missing for command: 'delete'.")
            .ExecuteAsync();
    }


    [Test]
    [Category("gsh")]
    [Category("gsh build")]
    [Category("gsh build delete")]
    public async Task BuildDelete_ThrowsInvalidBuildIdException()
    {
        await GetFullySetCli()
            .Command("gsh build delete a")
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardErrorContains("Build ID 'a' not a valid ID.")
            .ExecuteAsync();
    }

    [Test]
    [Category("gsh")]
    [Category("gsh build")]
    [Category("gsh build delete")]
    public async Task BuildDelete_ThrowsNotLoggedInException()
    {
        SetConfigValue("project-id", CommonKeys.ValidProjectId);
        SetConfigValue("environment-id", CommonKeys.ValidEnvironmentId);
        SetConfigValue("environment-name", CommonKeys.ValidEnvironmentName);

        await new UgsCliTestCase()
            .Command(k_BuildDeleteCommand)
            .AssertStandardErrorContains(k_NotLoggedIn)
            .AssertExitCode(ExitCode.HandledError)
            .ExecuteAsync();
    }

    [Test]
    [Category("gsh")]
    [Category("gsh build")]
    [Category("gsh build delete")]
    public async Task BuildDelete_ThrowsProjectIdNotSetException()
    {
        SetConfigValue("environment-id", CommonKeys.ValidEnvironmentId);
        SetConfigValue("environment-name", CommonKeys.ValidEnvironmentName);
        await GetLoggedInCli()
            .Command(k_BuildDeleteCommand)
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardErrorContains(k_ProjectIdIsNotSet)
            .ExecuteAsync();
    }

    [Test]
    [Category("gsh")]
    [Category("gsh build")]
    [Category("gsh build delete")]
    public async Task BuildDelete_ThrowsEnvironmentIdNotSetException()
    {
        SetConfigValue("project-id", CommonKeys.ValidProjectId);
        await GetLoggedInCli()
            .Command(k_BuildDeleteCommand)
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardErrorContains(k_EnvironmentNameIsNotSet)
            .ExecuteAsync();
    }
}
