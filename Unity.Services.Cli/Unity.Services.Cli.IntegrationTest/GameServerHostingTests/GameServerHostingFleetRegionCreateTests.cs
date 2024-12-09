using System.Threading.Tasks;
using NUnit.Framework;
using Unity.Services.Cli.Common.Exceptions;
using Unity.Services.Cli.IntegrationTest.Common;
using Unity.Services.Cli.MockServer.Common;
using Unity.Services.Cli.MockServer.ServiceMocks.GameServerHosting;

namespace Unity.Services.Cli.IntegrationTest.GameServerHostingTests;

public partial class GameServerHostingTests
{
    internal const string FleetRegionCreateCommand =
        $"mh fleet-region create --fleet-id {Keys.ValidFleetId} --region-id {Keys.ValidRegionId} --min-available-servers 1 --max-servers 2";

    [Test]
    [Category("mh")]
    [Category("mh fleet region")]
    [Category("mh fleet region create")]
    public async Task FleetRegionCreate_SucceedsWithValidInput()
    {
        await GetFullySetCli()
            .Command(FleetRegionCreateCommand)
            .AssertStandardOutput(
                v =>
                {
                    StringAssert.Contains("Creating fleet region...", v);
                    StringAssert.Contains("fleetRegionId: 00000000-0000-0000-0000-200000000000", v);
                })
            .AssertNoErrors()
            .ExecuteAsync();
    }

    [Test]
    [Category("mh")]
    [Category("mh fleet region")]
    [Category("mh fleet region create")]
    public async Task FleetRegionCreate_ThrowsMissingFleetException()
    {
        await GetFullySetCli()
            .Command($"mh fleet-region create --region-id {Keys.ValidRegionId} --min-available-servers 1 --max-servers 2")
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardErrorContains("Option '--fleet-id' is required.")
            .ExecuteAsync();
    }

    [Test]
    [Category("mh")]
    [Category("mh fleet region")]
    [Category("mh fleet region create")]
    public async Task FleetRegionCreate_ThrowsMissingRegionException()
    {
        await GetFullySetCli()
            .Command($"mh fleet-region create --fleet-id {Keys.ValidFleetId} --min-available-servers 1 --max-servers 2")
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardErrorContains("Option '--region-id' is required.")
            .ExecuteAsync();
    }


    [Test]
    [Category("mh")]
    [Category("mh fleet region")]
    [Category("mh fleet region create")]
    public async Task FleetRegionCreate_ThrowsMissingMinServersException()
    {
        await GetFullySetCli()
            .Command($"mh fleet-region create --fleet-id {Keys.ValidFleetId} --region-id {Keys.ValidRegionId} --max-servers 2")
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardErrorContains("Option '--min-available-servers' is required.")
            .ExecuteAsync();
    }


    [Test]
    [Category("mh")]
    [Category("mh fleet region")]
    [Category("mh fleet region create")]
    public async Task FleetRegionCreate_ThrowsMissingMaxServersException()
    {
        await GetFullySetCli()
            .Command($"mh fleet-region create --fleet-id {Keys.ValidFleetId} --region-id {Keys.ValidRegionId} --min-available-servers 1")
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardErrorContains("Option '--max-servers' is required.")
            .ExecuteAsync();
    }

    [Test]
    [Category("mh")]
    [Category("mh fleet region")]
    [Category("mh fleet region create")]
    public async Task FleetRegionCreate_ThrowsInvalidFleetException()
    {
        await GetFullySetCli()
            .Command($"mh fleet-region create --fleet-id invalid --region-id {Keys.ValidRegionId} --min-available-servers 1 --max-servers 2")
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardErrorContains("Cannot parse argument 'invalid' for option '--fleet-id'")
            .ExecuteAsync();
    }


    [Test]
    [Category("mh")]
    [Category("mh fleet region")]
    [Category("mh fleet region create")]
    public async Task FleetRegionCreate_ThrowsInvalidRegionException()
    {
        await GetFullySetCli()
            .Command($"mh fleet-region create --fleet-id {Keys.ValidFleetId} --region-id invalid --min-available-servers 1 --max-servers 2")
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardErrorContains("Cannot parse argument 'invalid' for option '--region-id'")
            .ExecuteAsync();
    }

    [Test]
    [Category("mh")]
    [Category("mh fleet region")]
    [Category("mh fleet region create")]
    public async Task FleetRegionCreate_ThrowsInvalidMinAvailableException()
    {
        await GetFullySetCli()
            .Command($"mh fleet-region create --fleet-id {Keys.ValidFleetId} --region-id {Keys.ValidRegionId}  --min-available-servers ABC --max-servers 2")
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardErrorContains("Cannot parse argument 'ABC' for option '--min-available-servers'")
            .ExecuteAsync();
    }

    [Test]
    [Category("mh")]
    [Category("mh fleet region")]
    [Category("mh fleet region create")]
    public async Task FleetRegionCreate_ThrowsInvalidMaxServersException()
    {
        await GetFullySetCli()
            .Command($"mh fleet-region create --fleet-id {Keys.ValidFleetId} --region-id {Keys.ValidRegionId}  --min-available-servers 1 --max-servers ABC")
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardErrorContains("Cannot parse argument 'ABC' for option '--max-servers'")
            .ExecuteAsync();
    }

    [Test]
    [Category("mh")]
    [Category("mh fleet region")]
    [Category("mh fleet region create")]
    public async Task FleetRegionCreate_ThrowsNotLoggedInException()
    {
        SetConfigValue("project-id", CommonKeys.ValidProjectId);
        SetConfigValue("environment-id", CommonKeys.ValidEnvironmentId);
        SetConfigValue("environment-name", CommonKeys.ValidEnvironmentName);

        await new UgsCliTestCase()
            .Command(FleetRegionCreateCommand)
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardErrorContains(k_NotLoggedIn)
            .ExecuteAsync();
    }

    [Test]
    [Category("mh")]
    [Category("mh fleet region")]
    [Category("mh fleet region create")]
    public async Task FleetRegionCreate_ThrowsProjectIdNotSetException()
    {
        SetConfigValue("environment-id", CommonKeys.ValidEnvironmentId);
        SetConfigValue("environment-name", CommonKeys.ValidEnvironmentName);
        await GetLoggedInCli()
            .Command(FleetRegionCreateCommand)
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardErrorContains(k_ProjectIdIsNotSet)
            .ExecuteAsync();
    }

    [Test]
    [Category("mh")]
    [Category("mh fleet region")]
    [Category("mh fleet region create")]
    public async Task FleetRegionCreate_ThrowsEnvironmentIdNotSetException()
    {
        SetConfigValue("project-id", CommonKeys.ValidProjectId);
        await GetLoggedInCli()
            .Command(FleetRegionCreateCommand)
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardErrorContains(k_EnvironmentNameIsNotSet)
            .ExecuteAsync();
    }
}
