using System.Threading.Tasks;
using NUnit.Framework;
using Unity.Services.Cli.Common.Exceptions;
using Unity.Services.Cli.IntegrationTest.Common;
using Unity.Services.Cli.MockServer.Common;

namespace Unity.Services.Cli.IntegrationTest.GameServerHostingTests;

public partial class GameServerHostingTests
{
    const string k_BuildUpdateCommand = "gsh build update 1 --name \"Updated Name\"";

    [Test]
    [Category("gsh")]
    [Category("gsh build")]
    [Category("gsh build update")]
    public async Task BuildUpdate_Succeeds()
    {
        await GetFullySetCli()
            .Command(k_BuildUpdateCommand)
            .AssertStandardOutputContains("Updating build...")
            .AssertStandardErrorContains("Build updated successfully")
            .AssertExitCode(ExitCode.Success)
            .ExecuteAsync();
    }

    [Test]
    [Category("gsh")]
    [Category("gsh build")]
    [Category("gsh build update")]
    public async Task BuildUpdate_ThrowsNotLoggedInException()
    {
        SetConfigValue("project-id", CommonKeys.ValidProjectId);
        SetConfigValue("environment-id", CommonKeys.ValidEnvironmentId);
        SetConfigValue("environment-name", CommonKeys.ValidEnvironmentName);

        await new UgsCliTestCase()
            .Command(k_BuildCreateCommand)
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardErrorContains(k_NotLoggedIn)
            .ExecuteAsync();
    }


    [Test]
    [Category("gsh")]
    [Category("gsh build")]
    [Category("gsh build update")]
    public async Task BuildUpdate_ThrowsProjectIdNotSetException()
    {
        SetConfigValue("environment-id", CommonKeys.ValidEnvironmentId);
        SetConfigValue("environment-name", CommonKeys.ValidEnvironmentName);
        await GetLoggedInCli()
            .Command(k_BuildUpdateCommand)
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardErrorContains(k_ProjectIdIsNotSet)
            .ExecuteAsync();
    }

    [Test]
    [Category("gsh")]
    [Category("gsh build")]
    [Category("gsh build update")]
    public async Task BuildUpdate_ThrowsEnvironmentIdNotSetException()
    {
        SetConfigValue("project-id", CommonKeys.ValidProjectId);
        await GetLoggedInCli()
            .Command(k_BuildUpdateCommand)
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardErrorContains(k_EnvironmentNameIsNotSet)
            .ExecuteAsync();
    }
}
