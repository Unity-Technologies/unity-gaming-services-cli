using System;
using System.IO;
using System.Threading.Tasks;
using NUnit.Framework;
using SharpYaml.Tokens;
using Unity.Services.Cli.Common.Exceptions;
using Unity.Services.Cli.IntegrationTest.Common;
using Unity.Services.Cli.MockServer.Common;
using Unity.Services.Cli.MockServer.ServiceMocks.GameServerHosting;

namespace Unity.Services.Cli.IntegrationTest.GameServerHostingTests;

public partial class GameServerHostingTests
{
    // Get the operating system's temporary directory
    static readonly string k_TempDirectory = Path.GetTempPath();
    static readonly string k_OutputArgPath = $"{k_TempDirectory}/server.log";

    static readonly string k_ServerFilesDownloadCommand = $"mh server files download --server-id {Keys.ValidServerId} --path {Keys.ValidErrorLogPath} --output {k_OutputArgPath}";

    [Test]
    [Category("mh")]
    [Category("mh server")]
    [Category("mh server files download")]
    public async Task ServerFilesDownload_Succeeds()
    {
        await GetFullySetCli()
            .Command(k_ServerFilesDownloadCommand)
            .AssertStandardOutput(
                str =>
                {
                    Assert.IsTrue(str.Contains("Downloading file..."));
                })
            .AssertStandardError(
                str =>
                {
                    Assert.IsTrue(str.Contains($"File downloaded to {k_OutputArgPath}"));
                    string fileContents = File.ReadAllText(k_OutputArgPath);
                    Assert.AreEqual(Keys.MockFileContent, fileContents);
                }
                )
            .ExecuteAsync();
    }

    [Test]
    [Category("mh")]
    [Category("mh server")]
    [Category("mh server files download")]
    public async Task ServerFilesDownload_ThrowsNotLoggedInException()
    {
        SetConfigValue("project-id", CommonKeys.ValidProjectId);
        SetConfigValue("environment-id", CommonKeys.ValidEnvironmentId);
        SetConfigValue("environment-name", CommonKeys.ValidEnvironmentName);

        await new UgsCliTestCase()
            .Command(k_ServerFilesDownloadCommand)
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardErrorContains(k_NotLoggedIn)
            .ExecuteAsync();
    }

    [Test]
    [Category("mh")]
    [Category("mh server")]
    [Category("mh server files download")]
    public async Task ServerFilesDownload_ThrowsProjectIdNotSetException()
    {
        SetConfigValue("environment-id", CommonKeys.ValidEnvironmentId);
        SetConfigValue("environment-name", CommonKeys.ValidEnvironmentName);
        await GetLoggedInCli()
            .Command(k_ServerFilesDownloadCommand)
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardErrorContains(k_ProjectIdIsNotSet)
            .ExecuteAsync();
    }

    [Test]
    [Category("mh")]
    [Category("mh server")]
    [Category("mh server files download")]
    public async Task ServerFilesDownload_ThrowsEnvironmentIdNotSetException()
    {
        SetConfigValue("project-id", CommonKeys.ValidProjectId);
        await GetLoggedInCli()
            .Command(k_ServerFilesDownloadCommand)
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardErrorContains(k_EnvironmentNameIsNotSet)
            .ExecuteAsync();
    }
}
