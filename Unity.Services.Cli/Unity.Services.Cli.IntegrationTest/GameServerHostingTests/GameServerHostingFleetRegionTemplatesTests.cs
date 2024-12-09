using System.Threading.Tasks;
using NUnit.Framework;
using Unity.Services.Cli.Common.Exceptions;
using Unity.Services.Cli.IntegrationTest.Common;
using Unity.Services.Cli.MockServer.Common;
using Unity.Services.Cli.MockServer.ServiceMocks.GameServerHosting;

namespace Unity.Services.Cli.IntegrationTest.GameServerHostingTests;

public partial class GameServerHostingTests
{
    internal const string FleetRegionTemplatesCommand = $"mh fleet-region templates";

    [Test]
    [Category("mh")]
    [Category("mh fleet region")]
    [Category("mh fleet region templates")]
    public async Task FleetRegionTemplates_SucceedsWithValidInput()
    {
        await GetFullySetCli()
            .Command(FleetRegionTemplatesCommand)
            .AssertStandardOutput(
                v =>
                {
                    StringAssert.Contains("Fetching region list...", v);
                    StringAssert.Contains("name: Example Region", v);
                })
            .AssertNoErrors()
            .ExecuteAsync();
    }

    [Test]
    [Category("mh")]
    [Category("mh fleet region")]
    [Category("mh fleet region templates")]
    public async Task FleetRegionTemplates_ThrowsNotLoggedInException()
    {
        SetConfigValue("project-id", CommonKeys.ValidProjectId);
        SetConfigValue("environment-id", CommonKeys.ValidEnvironmentId);
        SetConfigValue("environment-name", CommonKeys.ValidEnvironmentName);

        await new UgsCliTestCase()
            .Command(FleetRegionTemplatesCommand)
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardErrorContains(k_NotLoggedIn)
            .ExecuteAsync();
    }

    [Test]
    [Category("mh")]
    [Category("mh fleet region")]
    [Category("mh fleet region templates")]
    public async Task FleetRegionTemplates_ThrowsProjectIdNotSetException()
    {
        SetConfigValue("environment-id", CommonKeys.ValidEnvironmentId);
        SetConfigValue("environment-name", CommonKeys.ValidEnvironmentName);
        await GetLoggedInCli()
            .Command(FleetRegionTemplatesCommand)
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardErrorContains(k_ProjectIdIsNotSet)
            .ExecuteAsync();
    }

    [Test]
    [Category("mh")]
    [Category("mh fleet region")]
    [Category("mh fleet region templates")]
    public async Task FleetRegionTemplates_ThrowsEnvironmentIdNotSetException()
    {
        SetConfigValue("project-id", CommonKeys.ValidProjectId);
        await GetLoggedInCli()
            .Command(FleetRegionTemplatesCommand)
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardErrorContains(k_EnvironmentNameIsNotSet)
            .ExecuteAsync();
    }
}
