using System.Threading.Tasks;
using NUnit.Framework;
using Unity.Services.Cli.Common.Exceptions;
using Unity.Services.Cli.IntegrationTest.Common;
using Unity.Services.Cli.MockServer.Common;

namespace Unity.Services.Cli.IntegrationTest.GameServerHostingTests;

public partial class GameServerHostingTests
{
    static readonly string k_BuildConfigurationGetCommand = "mh bc get 1";

    [Test]
    [Category("mh")]
    [Category("mh bc")]
    [Category("mh bc get")]
    public async Task BuildConfigurationGet_SucceedsWithValidInput()
    {
        await GetFullySetCli()
            .Command(k_BuildConfigurationGetCommand)
            .AssertStandardOutput(
                v =>
                {
                    StringAssert.Contains("Fetching build configuration...", v);
                    StringAssert.Contains("binaryPath: ", v);
                })
            .AssertNoErrors()
            .ExecuteAsync();
    }

    [Test]
    [Category("mh")]
    [Category("mh bc")]
    [Category("mh bc get")]
    public async Task BuildConfigurationGet_ThrowsMissingBuildConfigurationIdException()
    {
        await GetFullySetCli()
            .Command("mh bc get")
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardErrorContains("Required argument missing for command: 'get'.")
            .ExecuteAsync();
    }

    [Test]
    [Category("mh")]
    [Category("mh bc")]
    [Category("mh bc get")]
    public async Task BuildConfigurationGet_ThrowsInvalidBuildConfigurationIdException()
    {
        await GetFullySetCli()
            .Command("mh bc get invalid-id")
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardErrorContains("Build Configuration ID 'invalid-id' not a valid ID.")
            .ExecuteAsync();
    }

    [Test]
    [Category("mh")]
    [Category("mh bc")]
    [Category("mh bc get")]
    public async Task BuildConfigurationGet_ThrowsNotLoggedInException()
    {
        SetConfigValue("project-id", CommonKeys.ValidProjectId);
        SetConfigValue("environment-id", CommonKeys.ValidEnvironmentId);
        SetConfigValue("environment-name", CommonKeys.ValidEnvironmentName);

        await new UgsCliTestCase()
            .Command(k_BuildConfigurationGetCommand)
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardErrorContains(k_NotLoggedIn)
            .ExecuteAsync();
    }

    [Test]
    [Category("mh")]
    [Category("mh bc")]
    [Category("mh bc get")]
    public async Task BuildConfigurationGet_ThrowsProjectIdNotSetException()
    {
        SetConfigValue("environment-id", CommonKeys.ValidEnvironmentId);
        SetConfigValue("environment-name", CommonKeys.ValidEnvironmentName);

        await GetLoggedInCli()
            .Command(k_BuildConfigurationGetCommand)
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardErrorContains(k_ProjectIdIsNotSet)
            .ExecuteAsync();
    }

    [Test]
    [Category("mh")]
    [Category("mh bc")]
    [Category("mh bc get")]
    public async Task BuildConfigurationGet_ThrowsEnvironmentIdNotSetException()
    {
        SetConfigValue("project-id", CommonKeys.ValidProjectId);

        await GetLoggedInCli()
            .Command(k_BuildConfigurationGetCommand)
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardErrorContains(k_EnvironmentNameIsNotSet)
            .ExecuteAsync();
    }
}
