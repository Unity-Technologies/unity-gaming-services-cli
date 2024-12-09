using System.Threading.Tasks;
using NUnit.Framework;
using Unity.Services.Cli.Common.Exceptions;
using Unity.Services.Cli.IntegrationTest.Common;
using Unity.Services.Cli.MockServer.Common;

namespace Unity.Services.Cli.IntegrationTest.GameServerHostingTests;

public partial class GameServerHostingTests
{
    const string k_BuildListCommand = "mh build list";

    [Test]
    [Category("mh")]
    [Category("mh build")]
    [Category("mh build list")]
    public async Task BuildList_Succeeds()
    {
        await GetFullySetCli()
            .Command(k_BuildListCommand)
            .AssertStandardOutput(
                // ReSharper disable once ParameterOnlyUsedForPreconditionCheck.Local
                str =>
                {
                    Assert.IsTrue(str.Contains("Fetching build list..."));
                    Assert.IsTrue(str.Contains("buildName: Build1"));
                    Assert.IsTrue(str.Contains("buildName: Build2"));
                })
            .AssertNoErrors()
            .ExecuteAsync();
    }

    [Test]
    [Category("mh")]
    [Category("mh build")]
    [Category("mh build list")]
    public async Task BuildList_ThrowsNotLoggedInException()
    {
        SetConfigValue("project-id", CommonKeys.ValidProjectId);
        SetConfigValue("environment-id", CommonKeys.ValidEnvironmentId);
        SetConfigValue("environment-name", CommonKeys.ValidEnvironmentName);

        await new UgsCliTestCase()
            .Command(k_BuildListCommand)
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardErrorContains(k_NotLoggedIn)
            .ExecuteAsync();
    }


    [Test]
    [Category("mh")]
    [Category("mh build")]
    [Category("mh build list")]
    public async Task BuildList_ThrowsProjectIdNotSetException()
    {
        SetConfigValue("environment-id", CommonKeys.ValidEnvironmentId);
        SetConfigValue("environment-name", CommonKeys.ValidEnvironmentName);
        await GetLoggedInCli()
            .Command(k_BuildListCommand)
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardErrorContains(k_ProjectIdIsNotSet)
            .ExecuteAsync();
    }

    [Test]
    [Category("mh")]
    [Category("mh build")]
    [Category("mh build list")]
    public async Task BuildList_ThrowsEnvironmentIdNotSetException()
    {
        SetConfigValue("project-id", CommonKeys.ValidProjectId);
        await GetLoggedInCli()
            .Command(k_BuildListCommand)
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardErrorContains(k_EnvironmentNameIsNotSet)
            .ExecuteAsync();
    }
}
