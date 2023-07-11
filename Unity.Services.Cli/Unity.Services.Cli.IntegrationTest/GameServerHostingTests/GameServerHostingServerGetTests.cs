using System.Threading.Tasks;
using NUnit.Framework;
using Unity.Services.Cli.Common.Exceptions;
using Unity.Services.Cli.IntegrationTest.Common;
using Unity.Services.Cli.MockServer.Common;
using Unity.Services.Cli.MockServer.ServiceMocks.GameServerHosting;

namespace Unity.Services.Cli.IntegrationTest.GameServerHostingTests;

public partial class GameServerHostingTests
{
    static readonly string k_ServersGetCommand = $"gsh server get {Keys.ValidServerId}";

    [Test]
    [Category("gsh")]
    [Category("gsh server")]
    [Category("gsh server get")]
    public async Task ServerGet_SucceedsWithValidInput()
    {
        await GetFullySetCli()
            .Command(k_ServersGetCommand)
            .AssertNoErrors()
            .ExecuteAsync();
    }

    [Test]
    [Category("gsh")]
    [Category("gsh server")]
    [Category("gsh server get")]
    public async Task ServerGet_Succeeds()
    {
        await GetFullySetCli()
            .Command(k_ServersGetCommand)
            .AssertStandardOutput(
                // ReSharper disable once ParameterOnlyUsedForPreconditionCheck.Local
                str =>
                {
                    Assert.IsTrue(str.Contains("Fetching server..."));
                    Assert.IsTrue(str.Contains($"id: {Keys.ValidServerId}"));
                })
            .AssertNoErrors()
            .ExecuteAsync();
    }

    [Test]
    [Category("gsh")]
    [Category("gsh server")]
    [Category("gsh server get")]
    public async Task ServerGet_ThrowsNotLoggedInException()
    {
        SetConfigValue("project-id", CommonKeys.ValidProjectId);
        SetConfigValue("environment-id", CommonKeys.ValidEnvironmentId);
        SetConfigValue("environment-name", CommonKeys.ValidEnvironmentName);

        await new UgsCliTestCase()
            .Command(k_ServersGetCommand)
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardErrorContains(k_NotLoggedIn)
            .ExecuteAsync();
    }

    [Test]
    [Category("gsh")]
    [Category("gsh server")]
    [Category("gsh server get")]
    public async Task ServerGet_ThrowsProjectIdNotSetException()
    {
        SetConfigValue("environment-id", CommonKeys.ValidEnvironmentId);
        SetConfigValue("environment-name", CommonKeys.ValidEnvironmentName);

        await GetLoggedInCli()
            .Command(k_ServersGetCommand)
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardErrorContains(k_ProjectIdIsNotSet)
            .ExecuteAsync();
    }

    [Test]
    [Category("gsh")]
    [Category("gsh server")]
    [Category("gsh server get")]
    public async Task ServerGet_ThrowsEnvironmentIdNotSetException()
    {
        SetConfigValue("project-id", CommonKeys.ValidProjectId);
        await GetLoggedInCli()
            .Command(k_ServersGetCommand)
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardErrorContains(k_EnvironmentNameIsNotSet)
            .ExecuteAsync();
    }

    [Test]
    [Category("gsh")]
    [Category("gsh server")]
    [Category("gsh server get")]
    public async Task ServerGet_ThrowsServerIdNotValidException()
    {
        SetConfigValue("project-id", CommonKeys.ValidProjectId);
        await GetLoggedInCli()
            .Command("gsh server get invalid-server-id")
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardErrorContains("Server ID 'invalid-server-id' not a valid ID")
            .ExecuteAsync();
    }
}
