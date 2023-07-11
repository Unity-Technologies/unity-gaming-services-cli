using System.Threading.Tasks;
using NUnit.Framework;
using Unity.Services.Cli.Common.Exceptions;
using Unity.Services.Cli.IntegrationTest.Common;
using Unity.Services.Cli.MockServer.Common;
using Unity.Services.Cli.MockServer.ServiceMocks.GameServerHosting;

namespace Unity.Services.Cli.IntegrationTest.GameServerHostingTests;

public partial class GameServerHostingTests
{
    static readonly string k_ServerListCommand = $"gsh server list --fleet-id {Keys.ValidFleetId}";

    [Test]
    [Category("gsh")]
    [Category("gsh server")]
    [Category("gsh server list")]
    public async Task ServerList_SucceedsWithValidInput()
    {
        await GetFullySetCli()
            .Command(k_ServerListCommand)
            .AssertNoErrors()
            .ExecuteAsync();
    }

    [Test]
    [Category("gsh")]
    [Category("gsh server")]
    [Category("gsh server list")]
    public async Task ServerList_Succeeds()
    {
        await GetFullySetCli()
            .Command(k_ServerListCommand)
            .AssertStandardOutput(
                // ReSharper disable once ParameterOnlyUsedForPreconditionCheck.Local
                str =>
                {
                    Assert.IsTrue(str.Contains("Fetching server list..."));
                    Assert.IsTrue(str.Contains("id: 123"));
                })
            .AssertNoErrors()
            .ExecuteAsync();
    }

    [Test]
    [Category("gsh")]
    [Category("gsh server")]
    [Category("gsh server list")]
    public async Task ServerList_ThrowsNotLoggedInException()
    {
        SetConfigValue("project-id", CommonKeys.ValidProjectId);
        SetConfigValue("environment-id", CommonKeys.ValidEnvironmentId);
        SetConfigValue("environment-name", CommonKeys.ValidEnvironmentName);

        await new UgsCliTestCase()
            .Command(k_ServerListCommand)
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardErrorContains(k_NotLoggedIn)
            .ExecuteAsync();
    }

    [Test]
    [Category("gsh")]
    [Category("gsh server")]
    [Category("gsh server list")]
    public async Task ServerList_ThrowsProjectIdNotSetException()
    {
        SetConfigValue("environment-id", CommonKeys.ValidEnvironmentId);
        SetConfigValue("environment-name", CommonKeys.ValidEnvironmentName);

        await GetLoggedInCli()
            .Command(k_ServerListCommand)
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardErrorContains(k_ProjectIdIsNotSet)
            .ExecuteAsync();
    }

    [Test]
    [Category("gsh")]
    [Category("gsh server")]
    [Category("gsh server list")]
    public async Task ServerList_ThrowsEnvironmentIdNotSetException()
    {
        SetConfigValue("project-id", CommonKeys.ValidProjectId);
        await GetLoggedInCli()
            .Command(k_ServerListCommand)
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardErrorContains(k_EnvironmentNameIsNotSet)
            .ExecuteAsync();
    }

    [Test]
    [Category("gsh")]
    [Category("gsh server")]
    [Category("gsh server list")]
    public async Task ServerList_ThrowsFleetIdNotValidException()
    {
        SetConfigValue("project-id", CommonKeys.ValidProjectId);
        await GetLoggedInCli()
            .Command("gsh server list --fleet-id invalid-fleet-id")
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardErrorContains("Invalid option for --fleet-id. invalid-fleet-id is not a valid UUID")
            .ExecuteAsync();
    }

    [Test]
    [Category("gsh")]
    [Category("gsh server")]
    [Category("gsh server list")]
    public async Task ServerList_ThrowsBuildConfigurationIdNotValid()
    {
        SetConfigValue("project-id", CommonKeys.ValidProjectId);
        await GetLoggedInCli()
            .Command(
                $"gsh server list --fleet-id {Keys.ValidFleetId} --build-configuration-id invalid-build-configuration-id")
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardErrorContains(
                "Invalid option for --build-configuration-id. invalid-build-configuration-id is not a valid number.")
            .ExecuteAsync();
    }
}
