using System.Threading.Tasks;
using NUnit.Framework;
using Unity.Services.Cli.Common.Exceptions;
using Unity.Services.Cli.Common.Models;
using Unity.Services.Cli.Common.Networking;
using Unity.Services.Cli.IntegrationTest.Common;
using Unity.Services.Cli.MockServer;
using Unity.Services.Cli.MockServer.Common;

namespace Unity.Services.Cli.IntegrationTest.LeaderboardTests;

public class LeaderboardSetupFailureTests : UgsCliFixture
{
    const string k_ProjectIdNotSetErrorMessage = "'project-id' is not set in project configuration."
                                                 + " '" + Keys.EnvironmentKeys.ProjectId + "' is not set in system environment variables.";
    const string k_LoggedOutErrorMessage = "You are not logged into any service account."
                                           + " Please login using the 'ugs login' command.";
    const string k_EnvironmentNameNotSetErrorMessage = "'environment-name' is not set in project configuration."
                                                       + " '" + Keys.EnvironmentKeys.EnvironmentName + "' is not set in system environment variables.";

    [SetUp]
    public void SetUp()
    {
        DeleteLocalConfig();
        DeleteLocalCredentials();
    }

    [TestCase("leaderboards list")]
    [TestCase("leaderboards delete foo-id")]
    [TestCase("leaderboards get foo-id")]
    [TestCase("leaderboards reset foo-id")]
    public async Task LeaderboardThrowsProjectIdNotSetException(string command)
    {
        SetConfigValue("environment-name", CommonKeys.ValidEnvironmentName);

        await GetLoggedInCli()
            .Command(command)
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardErrorContains(k_ProjectIdNotSetErrorMessage)
            .ExecuteAsync();
    }

    [TestCase("leaderboards list")]
    [TestCase("leaderboards delete foo-id")]
    [TestCase("leaderboards get foo-id")]
    [TestCase("leaderboards reset foo-id")]
    public async Task LeaderboardListThrowsNotLoggedInException(string command)
    {
        SetConfigValue("project-id", CommonKeys.ValidProjectId);
        SetConfigValue("environment-name", CommonKeys.ValidEnvironmentName);

        await new UgsCliTestCase()
            .Command(command)
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardErrorContains(k_LoggedOutErrorMessage)
            .ExecuteAsync();
    }

    [TestCase("leaderboards list")]
    [TestCase("leaderboards delete foo-id")]
    [TestCase("leaderboards get foo-id")]
    [TestCase("leaderboards reset foo-id")]
    public async Task LeaderboardListThrowsEnvironmentIdNotSetException(string command)
    {
        SetConfigValue("project-id", CommonKeys.ValidProjectId);

        await GetLoggedInCli()
            .Command(command)
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardErrorContains(k_EnvironmentNameNotSetErrorMessage)
            .ExecuteAsync();
    }
}

