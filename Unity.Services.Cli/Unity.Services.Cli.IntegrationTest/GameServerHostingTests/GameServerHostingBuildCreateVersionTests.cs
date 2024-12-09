using System.Threading.Tasks;
using NUnit.Framework;
using Unity.Services.Cli.Common.Exceptions;
using Unity.Services.Cli.IntegrationTest.Common;
using Unity.Services.Cli.MockServer.Common;

namespace Unity.Services.Cli.IntegrationTest.GameServerHostingTests;

public partial class GameServerHostingTests
{
    const string k_BuildCreateVersionCommand = "mh build create-version";
    const string k_BuildCreateVersionBucketArgs = " 101 --access-key test-access-key --secret-key test-secret-key --bucket-url test-bucket-url";
    const string k_BuildCreateVersionContainerArgs = " 102 --tag v1";

    [Test]
    [Category("mh")]
    [Category("mh build")]
    [Category("mh build create-version")]
    public async Task BuildCreateVersion_Bucket_SucceedsWithValidInput()
    {
        await GetFullySetCli()
            .Command(k_BuildCreateVersionCommand + k_BuildCreateVersionBucketArgs)
            .AssertStandardOutputContains("Creating build version...")
            .AssertStandardErrorContains("Build version created successfully")
            .ExecuteAsync();
    }

    [Test]
    [Category("mh")]
    [Category("mh build")]
    [Category("mh build create-version")]
    public async Task BuildCreateVersion_Container_SucceedsWithValidInput()
    {
        await GetFullySetCli()
            .Command(k_BuildCreateVersionCommand + k_BuildCreateVersionContainerArgs)
            .AssertStandardOutputContains("Creating build version...")
            .AssertStandardErrorContains("Build version created successfully")
            .ExecuteAsync();
    }

    [Test]
    [Category("mh")]
    [Category("mh build")]
    [Category("mh build create-version")]
    public async Task BuildCreateVersion_FileUpload_SucceedsWithValidInput()
    {
        var args = " 103 --directory " + m_TempDirectoryPath;
        await GetFullySetCli()
            .Command(k_BuildCreateVersionCommand + args)
            .AssertStandardOutputContains("Creating build version...")
            .AssertStandardErrorContains("Build version created successfully")
            .ExecuteAsync();
    }

    [Test]
    [Category("mh")]
    [Category("mh build")]
    [Category("mh build create-version")]
    public async Task BuildCreateVersion_ThrowsMissingIdException()
    {
        await GetFullySetCli()
            .Command("mh build create-version")
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardErrorContains("Required argument missing for command: 'create-version'.")
            .ExecuteAsync();
    }

    [Test]
    [Category("mh")]
    [Category("mh build")]
    [Category("mh build create-version")]
    public async Task BuildCreateVersion_ThrowsNotLoggedInException()
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
    [Category("mh")]
    [Category("mh build")]
    [Category("mh build create-version")]
    public async Task BuildCreateVersion_ThrowsProjectIdNotSetException()
    {
        SetConfigValue("environment-id", CommonKeys.ValidEnvironmentId);
        SetConfigValue("environment-name", CommonKeys.ValidEnvironmentName);
        await GetLoggedInCli()
            .Command(k_BuildCreateCommand)
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardErrorContains(k_ProjectIdIsNotSet)
            .ExecuteAsync();
    }

    [Test]
    [Category("mh")]
    [Category("mh build")]
    [Category("mh build create-version")]
    public async Task BuildCreateVersion_ThrowsEnvironmentIdNotSetException()
    {
        SetConfigValue("project-id", CommonKeys.ValidProjectId);
        await GetLoggedInCli()
            .Command(k_BuildCreateCommand)
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardErrorContains(k_EnvironmentNameIsNotSet)
            .ExecuteAsync();
    }
}
