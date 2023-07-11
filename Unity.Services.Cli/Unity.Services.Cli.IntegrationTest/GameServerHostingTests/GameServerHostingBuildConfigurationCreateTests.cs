using System.Threading.Tasks;
using NUnit.Framework;
using Unity.Services.Cli.Common.Exceptions;
using Unity.Services.Cli.IntegrationTest.Common;
using Unity.Services.Cli.MockServer.Common;

namespace Unity.Services.Cli.IntegrationTest.GameServerHostingTests;

public partial class GameServerHostingTests
{
    static readonly string k_BuildConfigurationCreatePrefix = "gsh bc create ";

    [Test]
    [Category("gsh")]
    [Category("gsh bc")]
    [Category("gsh bc create")]
    public async Task BuildConfigurationCreate_SucceedsWithValidInput()
    {
        await GetFullySetCli()
            .Command(k_BuildConfigurationCreatePrefix + k_BuildConfigurationCreateOrUpdateCommandComplete)
            .AssertStandardOutput(
                v =>
                {
                    StringAssert.Contains("Creating build config...", v);
                    StringAssert.Contains("binaryPath: ", v);
                })
            .AssertNoErrors()
            .ExecuteAsync();
    }

    [Test]
    [Category("gsh")]
    [Category("gsh bc")]
    [Category("gsh bc create")]
    public async Task BuildConfigurationCreate_ThrowsNotLoggedInException()
    {
        SetConfigValue("project-id", CommonKeys.ValidProjectId);
        SetConfigValue("environment-id", CommonKeys.ValidEnvironmentId);
        SetConfigValue("environment-name", CommonKeys.ValidEnvironmentName);

        await new UgsCliTestCase()
            .Command(k_BuildConfigurationCreatePrefix + k_BuildConfigurationCreateOrUpdateCommandComplete)
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardErrorContains(k_NotLoggedIn)
            .ExecuteAsync();
    }

    [Test]
    [Category("gsh")]
    [Category("gsh bc")]
    [Category("gsh bc create")]
    public async Task BuildConfigurationCreate_ThrowsProjectIdNotSetException()
    {
        SetConfigValue("environment-id", CommonKeys.ValidEnvironmentId);
        SetConfigValue("environment-name", CommonKeys.ValidEnvironmentName);

        await GetLoggedInCli()
            .Command(k_BuildConfigurationCreatePrefix + k_BuildConfigurationCreateOrUpdateCommandComplete)
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardErrorContains(k_ProjectIdIsNotSet)
            .ExecuteAsync();
    }

    [Test]
    [Category("gsh")]
    [Category("gsh bc")]
    [Category("gsh bc create")]
    public async Task BuildConfigurationCreate_ThrowsEnvironmentIdNotSetException()
    {
        SetConfigValue("project-id", CommonKeys.ValidProjectId);

        await GetLoggedInCli()
            .Command(k_BuildConfigurationCreatePrefix + k_BuildConfigurationCreateOrUpdateCommandComplete)
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardErrorContains(k_EnvironmentNameIsNotSet)
            .ExecuteAsync();
    }

    [Test]
    [Category("gsh")]
    [Category("gsh bc")]
    [Category("gsh bc create")]
    public async Task BuildConfigurationCreate_FailsMissingBinaryPathException()
    {
        await GetFullySetCli()
            .Command(k_BuildConfigurationCreatePrefix + k_BuildConfigurationCreateOrUpdateCommandMissingBinaryPath)
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardErrorContains("Option '--binary-path' is required.")
            .ExecuteAsync();
    }

    [Test]
    [Category("gsh")]
    [Category("gsh bc")]
    [Category("gsh bc create")]
    public async Task BuildConfigurationCreate_FailsMissingBuildException()
    {
        await GetFullySetCli()
            .Command(k_BuildConfigurationCreatePrefix + k_BuildConfigurationCreateOrUpdateCommandMissingBuild)
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardErrorContains("Option '--build' is required.")
            .ExecuteAsync();
    }

    [Test]
    [Category("gsh")]
    [Category("gsh bc")]
    [Category("gsh bc create")]
    public async Task BuildConfigurationCreate_FailsMissingCommandLineException()
    {
        await GetFullySetCli()
            .Command(k_BuildConfigurationCreatePrefix + k_BuildConfigurationCreateOrUpdateCommandMissingCommandLine)
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardErrorContains("Option '--command-line' is required.")
            .ExecuteAsync();
    }

    [Test]
    [Category("gsh")]
    [Category("gsh bc")]
    [Category("gsh bc create")]
    public async Task BuildConfigurationCreate_FailsMissingCoresException()
    {
        await GetFullySetCli()
            .Command(k_BuildConfigurationCreatePrefix + k_BuildConfigurationCreateOrUpdateCommandMissingCores)
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardErrorContains("Option '--cores' is required.")
            .ExecuteAsync();
    }

    [Test]
    [Category("gsh")]
    [Category("gsh bc")]
    [Category("gsh bc create")]
    public async Task BuildConfigurationCreate_FailsMissingMemoryException()
    {
        await GetFullySetCli()
            .Command(k_BuildConfigurationCreatePrefix + k_BuildConfigurationCreateOrUpdateCommandMissingMemory)
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardErrorContains("Option '--memory' is required.")
            .ExecuteAsync();
    }

    [Test]
    [Category("gsh")]
    [Category("gsh bc")]
    [Category("gsh bc create")]
    public async Task BuildConfigurationCreate_FailsMissingNameException()
    {
        await GetFullySetCli()
            .Command(k_BuildConfigurationCreatePrefix + k_BuildConfigurationCreateOrUpdateCommandMissingName)
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardErrorContains("Option '--name' is required.")
            .ExecuteAsync();
    }

    [Test]
    [Category("gsh")]
    [Category("gsh bc")]
    [Category("gsh bc create")]
    public async Task BuildConfigurationCreate_FailsMissingQueryTypeException()
    {
        await GetFullySetCli()
            .Command(k_BuildConfigurationCreatePrefix + k_BuildConfigurationCreateOrUpdateCommandMissingQueryType)
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardErrorContains("Option '--query-type' is required.")
            .ExecuteAsync();
    }

    [Test]
    [Category("gsh")]
    [Category("gsh bc")]
    [Category("gsh bc create")]
    public async Task BuildConfigurationCreate_FailsMissingSpeedException()
    {
        await GetFullySetCli()
            .Command(k_BuildConfigurationCreatePrefix + k_BuildConfigurationCreateOrUpdateCommandMissingSpeed)
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardErrorContains("Option '--speed' is required.")
            .ExecuteAsync();
    }
}
