using System.Threading.Tasks;
using NUnit.Framework;
using Unity.Services.Cli.Common.Exceptions;
using Unity.Services.Cli.IntegrationTest.Common;
using Unity.Services.Cli.MockServer.Common;

namespace Unity.Services.Cli.IntegrationTest.GameServerHostingTests;

public partial class GameServerHostingTests
{
    const string k_BuildGetCommand = "mh build get 1";

    [Test]
    [Category("mh")]
    [Category("mh build")]
    [Category("mh build get")]
    public async Task BuildGet_SucceedsWithValidInput()
    {
        await GetFullySetCli()
            .Command(k_BuildGetCommand)
            .AssertStandardOutput(
                // ReSharper disable once ParameterOnlyUsedForPreconditionCheck.Local
                str =>
                {
                    Assert.IsTrue(str.Contains("Fetching build..."));
                    Assert.IsTrue(str.Contains("buildName: name1"));
                })
            .AssertNoErrors()
            .ExecuteAsync();
    }

    [Test]
    [Category("mh")]
    [Category("mh build")]
    [Category("mh build get")]
    public async Task BuildGet_ThrowsMissingBuildIdException()
    {
        await GetFullySetCli()
            .Command("mh build get")
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardErrorContains("Required argument missing for command: 'get'.")
            .ExecuteAsync();
    }


    [Test]
    [Category("mh")]
    [Category("mh build")]
    [Category("mh build get")]
    public async Task BuildGet_ThrowsInvalidBuildIdException()
    {
        await GetFullySetCli()
            .Command("mh build get a")
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardErrorContains("Build ID 'a' not a valid ID.")
            .ExecuteAsync();
    }

    [Test]
    [Category("mh")]
    [Category("mh build")]
    [Category("mh build get")]
    public async Task BuildGet_ThrowsNotLoggedInException()
    {
        SetConfigValue("project-id", CommonKeys.ValidProjectId);
        SetConfigValue("environment-id", CommonKeys.ValidEnvironmentId);
        SetConfigValue("environment-name", CommonKeys.ValidEnvironmentName);

        await new UgsCliTestCase()
            .Command(k_BuildGetCommand)
            .AssertStandardErrorContains(k_NotLoggedIn)
            .AssertExitCode(ExitCode.HandledError)
            .ExecuteAsync();
    }

    [Test]
    [Category("mh")]
    [Category("mh build")]
    [Category("mh build get")]
    public async Task BuildGet_ThrowsProjectIdNotSetException()
    {
        SetConfigValue("environment-id", CommonKeys.ValidEnvironmentId);
        SetConfigValue("environment-name", CommonKeys.ValidEnvironmentName);
        await GetLoggedInCli()
            .Command(k_BuildGetCommand)
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardErrorContains(k_ProjectIdIsNotSet)
            .ExecuteAsync();
    }

    [Test]
    [Category("mh")]
    [Category("mh build")]
    [Category("mh build get")]
    public async Task BuildGet_ThrowsEnvironmentIdNotSetException()
    {
        SetConfigValue("project-id", CommonKeys.ValidProjectId);
        await GetLoggedInCli()
            .Command(k_BuildGetCommand)
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardErrorContains(k_EnvironmentNameIsNotSet)
            .ExecuteAsync();
    }
}
