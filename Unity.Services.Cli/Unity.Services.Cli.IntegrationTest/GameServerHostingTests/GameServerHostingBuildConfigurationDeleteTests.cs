using System.Threading.Tasks;
using NUnit.Framework;
using Unity.Services.Cli.Common.Exceptions;
using Unity.Services.Cli.IntegrationTest.Common;
using Unity.Services.Cli.MockServer.Common;

namespace Unity.Services.Cli.IntegrationTest.GameServerHostingTests;

public partial class GameServerHostingTests
{
    static readonly string k_BuildConfigurationDeleteCommand = "gsh bc delete 1";

    [Test]
    [Category("gsh")]
    [Category("gsh bc")]
    [Category("gsh bc delete")]
    public async Task BuildConfigurationDelete_SucceedsWithValidInput()
    {
        await GetFullySetCli()
            .Command(k_BuildConfigurationDeleteCommand)
            .AssertStandardOutputContains("Deleting build configuration...")
            .AssertStandardErrorContains("Build configuration deleted successfully")
            .ExecuteAsync();
    }

    [Test]
    [Category("gsh")]
    [Category("gsh bc")]
    [Category("gsh bc delete")]
    public async Task BuildConfigurationDelete_ThrowsMissingBuildConfigurationIdException()
    {
        await GetFullySetCli()
            .Command("gsh bc delete")
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardErrorContains("Required argument missing for command: 'delete'.")
            .ExecuteAsync();
    }

    [Test]
    [Category("gsh")]
    [Category("gsh bc")]
    [Category("gsh bc delete")]
    public async Task BuildConfigurationDelete_ThrowsNotLoggedInException()
    {
        SetConfigValue("project-id", CommonKeys.ValidProjectId);
        SetConfigValue("environment-id", CommonKeys.ValidEnvironmentId);
        SetConfigValue("environment-name", CommonKeys.ValidEnvironmentName);

        await new UgsCliTestCase()
            .Command(k_BuildConfigurationDeleteCommand)
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardErrorContains(k_NotLoggedIn)
            .ExecuteAsync();
    }

    [Test]
    [Category("gsh")]
    [Category("gsh bc")]
    [Category("gsh bc delete")]
    public async Task BuildConfigurationDelete_ThrowsProjectIdNotSetException()
    {
        SetConfigValue("environment-id", CommonKeys.ValidEnvironmentId);
        SetConfigValue("environment-name", CommonKeys.ValidEnvironmentName);

        await GetLoggedInCli()
            .Command(k_BuildConfigurationDeleteCommand)
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardErrorContains(k_ProjectIdIsNotSet)
            .ExecuteAsync();
    }

    [Test]
    [Category("gsh")]
    [Category("gsh bc")]
    [Category("gsh bc delete")]
    public async Task BuildConfigurationDelete_ThrowsEnvironmentIdNotSetException()
    {
        SetConfigValue("project-id", CommonKeys.ValidProjectId);

        await GetLoggedInCli()
            .Command(k_BuildConfigurationDeleteCommand)
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardErrorContains(k_EnvironmentNameIsNotSet)
            .ExecuteAsync();
    }
}
