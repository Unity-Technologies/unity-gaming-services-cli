using System.Threading.Tasks;
using NUnit.Framework;
using Unity.Services.Cli.Common.Exceptions;
using Unity.Services.Cli.IntegrationTest.Common;
using Unity.Services.Cli.MockServer.Common;
using Unity.Services.Cli.MockServer.ServiceMocks.GameServerHosting;

namespace Unity.Services.Cli.IntegrationTest.GameServerHostingTests;

public partial class GameServerHostingTests
{
    internal const string FleetRegionAvailableCommand = $"mh fleet-region available {Keys.ValidFleetId}";

    [Test]
    [Category("mh")]
    [Category("mh fleet region")]
    [Category("mh fleet region available")]
    public async Task FleetRegionAvailable_SucceedsWithValidInput()
    {
        await GetFullySetCli()
            .Command(FleetRegionAvailableCommand)
            .AssertStandardOutput(
                v =>
                {
                    StringAssert.Contains("Fetching available regions...", v);
                    StringAssert.Contains("name: US East", v);
                })
            .AssertNoErrors()
            .ExecuteAsync();
    }

    [Test]
    [Category("mh")]
    [Category("mh fleet region")]
    [Category("mh fleet region available")]
    public async Task FleetRegionAvailable_ThrowsInvalidFleetException()
    {
        await GetFullySetCli()
            .Command($"mh fleet-region available A")
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardErrorContains("Fleet 'A' not a valid UUID.")
            .ExecuteAsync();
    }

    [Test]
    [Category("mh")]
    [Category("mh fleet region")]
    [Category("mh fleet region available")]
    public async Task FleetRegionAvailable_ThrowsNotLoggedInException()
    {
        SetConfigValue("project-id", CommonKeys.ValidProjectId);
        SetConfigValue("environment-id", CommonKeys.ValidEnvironmentId);
        SetConfigValue("environment-name", CommonKeys.ValidEnvironmentName);

        await new UgsCliTestCase()
            .Command(FleetRegionAvailableCommand)
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardErrorContains(k_NotLoggedIn)
            .ExecuteAsync();
    }

    [Test]
    [Category("mh")]
    [Category("mh fleet region")]
    [Category("mh fleet region available")]
    public async Task FleetRegionAvailable_ThrowsProjectIdNotSetException()
    {
        SetConfigValue("environment-id", CommonKeys.ValidEnvironmentId);
        SetConfigValue("environment-name", CommonKeys.ValidEnvironmentName);
        await GetLoggedInCli()
            .Command(FleetRegionAvailableCommand)
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardErrorContains(k_ProjectIdIsNotSet)
            .ExecuteAsync();
    }

    [Test]
    [Category("mh")]
    [Category("mh fleet region")]
    [Category("mh fleet region available")]
    public async Task FleetRegionAvailable_ThrowsEnvironmentIdNotSetException()
    {
        SetConfigValue("project-id", CommonKeys.ValidProjectId);
        await GetLoggedInCli()
            .Command(FleetRegionAvailableCommand)
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardErrorContains(k_EnvironmentNameIsNotSet)
            .ExecuteAsync();
    }
}
