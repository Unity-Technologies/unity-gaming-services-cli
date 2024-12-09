using System.Threading.Tasks;
using NUnit.Framework;
using Unity.Services.Cli.Common.Exceptions;
using Unity.Services.Cli.IntegrationTest.Common;
using Unity.Services.Cli.MockServer.Common;

namespace Unity.Services.Cli.IntegrationTest.GameServerHostingTests;

public partial class GameServerHostingTests
{
    static readonly string k_BuildConfigurationListCommand = "mh bc list";

    [Test]
    [Category("mh")]
    [Category("mh bc")]
    [Category("mh bc list")]
    public async Task BuildConfigurationList_SucceedsWithValidInput()
    {
        await GetFullySetCli()
            .Command(k_BuildConfigurationListCommand)
            .AssertStandardOutput(
                v =>
                {
                    StringAssert.Contains("Fetching build configuration list...", v);
                    StringAssert.Contains("name: ", v);
                })
            .AssertNoErrors()
            .ExecuteAsync();
    }

    [Test]
    [Category("mh")]
    [Category("mh bc")]
    [Category("mh bc list")]
    public async Task BuildConfigurationList_ThrowsNotLoggedInException()
    {
        SetConfigValue("project-id", CommonKeys.ValidProjectId);
        SetConfigValue("environment-id", CommonKeys.ValidEnvironmentId);
        SetConfigValue("environment-name", CommonKeys.ValidEnvironmentName);

        await new UgsCliTestCase()
            .Command(k_BuildConfigurationListCommand)
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardErrorContains(k_NotLoggedIn)
            .ExecuteAsync();
    }

    [Test]
    [Category("mh")]
    [Category("mh bc")]
    [Category("mh bc list")]
    public async Task BuildConfigurationList_ThrowsProjectIdNotSetException()
    {
        SetConfigValue("environment-id", CommonKeys.ValidEnvironmentId);
        SetConfigValue("environment-name", CommonKeys.ValidEnvironmentName);

        await GetLoggedInCli()
            .Command(k_BuildConfigurationListCommand)
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardErrorContains(k_ProjectIdIsNotSet)
            .ExecuteAsync();
    }

    [Test]
    [Category("mh")]
    [Category("mh bc")]
    [Category("mh bc get")]
    public async Task BuildConfigurationList_ThrowsEnvironmentIdNotSetException()
    {
        SetConfigValue("project-id", CommonKeys.ValidProjectId);

        await GetLoggedInCli()
            .Command(k_BuildConfigurationListCommand)
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardErrorContains(k_EnvironmentNameIsNotSet)
            .ExecuteAsync();
    }
}
