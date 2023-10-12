using System;
using System.IO;
using System.Threading.Tasks;
using NUnit.Framework;
using Unity.Services.Cli.Common.Exceptions;
using Unity.Services.Cli.Common.Networking;
using Unity.Services.Cli.IntegrationTest.Common;
using Unity.Services.Cli.MockServer;
using Unity.Services.Cli.MockServer.Common;
using Unity.Services.Cli.MockServer.ServiceMocks;

namespace Unity.Services.Cli.IntegrationTest.Authoring.Deploy;

public class DeployPreconditionTests : UgsCliFixture
{
    [SetUp]
    public async Task SetUp()
    {
        DeleteLocalConfig();
        DeleteLocalCredentials();
        MockApi.Server?.ResetMappings();
        await MockApi.MockServiceAsync(new IdentityV1Mock());
    }

    [Test]
    public async Task DeployInvalidPath()
    {
        SetConfigValue("project-id", CommonKeys.ValidProjectId);
        SetConfigValue("environment-name", CommonKeys.ValidEnvironmentName);
        var invalidDirectory = Path.GetFullPath("invalid-directory");
        var expectedOutput = $"[Error]: {Environment.NewLine}"
                             + $"    Path \"{invalidDirectory}\" could not be found.{Environment.NewLine}";

        await GetLoggedInCli()
            .Command($"deploy {invalidDirectory}")
            .AssertStandardErrorContains(expectedOutput)
            .AssertExitCode(ExitCode.HandledError)
            .ExecuteAsync();
    }

    [Test]
    public async Task DeployWithoutEnvironmentConfig()
    {
        SetConfigValue("project-id", CommonKeys.ValidProjectId);
        await GetLoggedInCli()
            .Command($"deploy .")
            .AssertStandardErrorContains($"[Error]: {Environment.NewLine}    'environment-name' is not set")
            .AssertExitCode(ExitCode.HandledError)
            .ExecuteAsync();
    }

    [Test]
    public async Task DeployWithoutProjectConfig()
    {
        SetConfigValue("environment-name", CommonKeys.ValidEnvironmentName);
        await GetLoggedInCli()
            .Command($"deploy .")
            .AssertStandardErrorContains($"[Error]: {Environment.NewLine}    'project-id' is not set")
            .AssertExitCode(ExitCode.HandledError)
            .ExecuteAsync();
    }

    [Test]
    public async Task DeployWithoutLogin()
    {
        SetConfigValue("project-id", CommonKeys.ValidProjectId);
        SetConfigValue("environment-name", CommonKeys.ValidEnvironmentName);

        await new UgsCliTestCase()
            .Command($"deploy .")
            .AssertStandardErrorContains($"[Error]: {Environment.NewLine}    You are not logged into any service account. Please login using the 'ugs login' command.")
            .AssertExitCode(ExitCode.HandledError)
            .ExecuteAsync();
    }
}
