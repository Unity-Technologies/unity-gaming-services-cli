using System.Threading.Tasks;
using NUnit.Framework;
using Unity.Services.Cli.Common.Exceptions;
using Unity.Services.Cli.IntegrationTest.Common;
using Unity.Services.Cli.MockServer.Common;

namespace Unity.Services.Cli.IntegrationTest.GameServerHostingTests;

public partial class GameServerHostingTests
{
    const string k_BuildInstallsCommand = "gsh build installs 1";

    [Test]
    [Category("gsh")]
    [Category("gsh build")]
    [Category("gsh build installs")]
    public async Task BuildInstalls_SucceedsWithValidInput()
    {
        await GetFullySetCli()
            .Command(k_BuildInstallsCommand)
            .AssertNoErrors()
            .AssertStandardOutput(
                // ReSharper disable once ParameterOnlyUsedForPreconditionCheck.Local
                str =>
                {
                    Assert.IsTrue(str.Contains("Fetching installs..."));
                    Assert.IsTrue(str.Contains("fleetName: fleet name"));
                })
            .ExecuteAsync();
    }

    [Test]
    [Category("gsh")]
    [Category("gsh build")]
    [Category("gsh build installs")]
    public async Task BuildInstalls_ThrowsMissingBuildIdException()
    {
        await GetFullySetCli()
            .Command("gsh build installs")
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardErrorContains("Required argument missing for command: 'installs'.")
            .ExecuteAsync();
    }


    [Test]
    [Category("gsh")]
    [Category("gsh build")]
    [Category("gsh build installs")]
    public async Task BuildInstalls_ThrowsInvalidBuildIdException()
    {
        await GetFullySetCli()
            .Command("gsh build installs a")
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardErrorContains("Build ID 'a' not a valid ID.")
            .ExecuteAsync();
    }

    [Test]
    [Category("gsh")]
    [Category("gsh build")]
    [Category("gsh build installs")]
    public async Task BuildInstalls_ThrowsNotLoggedInException()
    {
        SetConfigValue("project-id", CommonKeys.ValidProjectId);
        SetConfigValue("environment-id", CommonKeys.ValidEnvironmentId);
        SetConfigValue("environment-name", CommonKeys.ValidEnvironmentName);

        await new UgsCliTestCase()
            .Command(k_BuildInstallsCommand)
            .AssertStandardErrorContains(k_NotLoggedIn)
            .AssertExitCode(ExitCode.HandledError)
            .ExecuteAsync();
    }

    [Test]
    [Category("gsh")]
    [Category("gsh build")]
    [Category("gsh build installs")]
    public async Task BuildInstalls_ThrowsProjectIdNotSetException()
    {
        SetConfigValue("environment-id", CommonKeys.ValidEnvironmentId);
        SetConfigValue("environment-name", CommonKeys.ValidEnvironmentName);
        await GetLoggedInCli()
            .Command(k_BuildInstallsCommand)
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardErrorContains(k_ProjectIdIsNotSet)
            .ExecuteAsync();
    }

    [Test]
    [Category("gsh")]
    [Category("gsh build")]
    [Category("gsh build installs")]
    public async Task BuildInstalls_ThrowsEnvironmentIdNotSetException()
    {
        SetConfigValue("project-id", CommonKeys.ValidProjectId);
        await GetLoggedInCli()
            .Command(k_BuildInstallsCommand)
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardErrorContains(k_EnvironmentNameIsNotSet)
            .ExecuteAsync();
    }
}
