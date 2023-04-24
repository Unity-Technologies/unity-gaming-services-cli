using System;
using System.IO;
using System.Threading.Tasks;
using NUnit.Framework;
using Unity.Services.Cli.Common.Exceptions;
using Unity.Services.Cli.Common.Models;
using Unity.Services.Cli.Common.Networking;
using Unity.Services.Cli.MockServer;
using Unity.Services.Cli.MockServer.Common;
using Unity.Services.Cli.MockServer.ServiceMocks;
using Unity.Services.Gateway.CloudCodeApiV1.Generated.Model;

namespace Unity.Services.Cli.IntegrationTest.CloudCodeTests;

public class CloudCodeScriptTests : UgsCliFixture
{
    const string k_ValidFilepath = @".\createintegrationtemp.js";
    const string k_ParsingTestFilesDirectory
        = "Unity.Services.Cli/Unity.Services.Cli.CloudCode.UnitTest/ScriptTestCases/JS";
    const string k_ValidScriptName = "test-3";
    const string k_ProjectIdNotSetErrorMessage = "'project-id' is not set in project configuration."
        + " '" + Keys.EnvironmentKeys.ProjectId + "' is not set in system environment variables.";
    const string k_LoggedOutErrorMessage = "You are not logged into any service account."
        + " Please login using the 'ugs login' command.";
    const string k_EnvironmentNameNotSetErrorMessage = "'environment-name' is not set in project configuration."
        + " '" + Keys.EnvironmentKeys.EnvironmentName + "' is not set in system environment variables.";
    const string k_UnauthorizedFileAccessErrorMessage = "Make sure that the CLI has the permissions to access"
        + " the file and that the specified path points to a file and not a directory.";

    [SetUp]
    public async Task SetUp()
    {
        DeleteLocalConfig();
        DeleteLocalCredentials();

        await m_MockApi.MockServiceAsync(new IdentityV1Mock());
        await m_MockApi.MockServiceAsync(new CloudCodeV1Mock());
    }

    [TearDown]
    public void TearDown()
    {
        File.Delete(k_ValidFilepath);
        m_MockApi.Server?.ResetMappings();
    }

    // cloud-code list tests
    [Test]
    public async Task CloudCodeListThrowsProjectIdNotSetException()
    {
        SetConfigValue("environment-name", CommonKeys.ValidEnvironmentName);

        await GetLoggedInCli()
            .Command("cloud-code list")
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardOutputContains(k_ProjectIdNotSetErrorMessage)
            .ExecuteAsync();
    }

    [Test]
    public async Task CloudCodeListThrowsNotLoggedInException()
    {
        SetConfigValue("project-id", CommonKeys.ValidProjectId);
        SetConfigValue("environment-name", CommonKeys.ValidEnvironmentName);

        await new UgsCliTestCase()
            .Command("cloud-code list")
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardOutputContains(k_LoggedOutErrorMessage)
            .ExecuteAsync();
    }

    [Test]
    public async Task CloudCodeListThrowsEnvironmentIdNotSetException()
    {
        SetConfigValue("project-id", CommonKeys.ValidProjectId);

        await GetLoggedInCli()
            .Command("cloud-code list")
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardOutputContains(k_EnvironmentNameNotSetErrorMessage)
            .ExecuteAsync();
    }

    [Test]
    public async Task CloudCodeListReturnsZeroExitCode()
    {
        var expectedMessage = $"example-string{Environment.NewLine}example-string{Environment.NewLine}example-string";
        SetConfigValue("project-id", CommonKeys.ValidProjectId);
        SetConfigValue("environment-name", CommonKeys.ValidEnvironmentName);

        await GetLoggedInCli()
            .Command("cloud-code list")
            .AssertNoErrors()
            .AssertStandardOutputContains(expectedMessage)
            .ExecuteAsync();
    }

    // cloud-code publish tests
    [Test]
    public async Task CloudCodePublishThrowsProjectIdNotSetException()
    {
        SetConfigValue("environment-name", CommonKeys.ValidEnvironmentName);

        await new UgsCliTestCase()
            .Command($"cloud-code publish {k_ValidScriptName}")
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardOutputContains(k_ProjectIdNotSetErrorMessage)
            .ExecuteAsync();
    }

    [Test]
    public async Task CloudCodePublishThrowsNotLoggedInException()
    {
        SetConfigValue("project-id", CommonKeys.ValidProjectId);
        SetConfigValue("environment-name", CommonKeys.ValidEnvironmentName);

        await new UgsCliTestCase()
            .Command("cloud-code publish test-script-123")
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardOutputContains(k_LoggedOutErrorMessage)
            .ExecuteAsync();
    }

    [Test]
    public async Task CloudCodePublishThrowsEnvironmentIdNotSetException()
    {
        SetConfigValue("project-id", CommonKeys.ValidProjectId);

        await GetLoggedInCli()
            .Command($"cloud-code publish {k_ValidScriptName}")
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardOutputContains(k_EnvironmentNameNotSetErrorMessage)
            .ExecuteAsync();
    }

    [Test]
    public async Task CloudCodePublishReturnsZeroExitCode()
    {
        const string expectedMsg = "Script version 42 published at 04/05/2022 09:12:13.";
        SetConfigValue("project-id", CommonKeys.ValidProjectId);
        SetConfigValue("environment-name", CommonKeys.ValidEnvironmentName);

        await GetLoggedInCli()
            .Command($"cloud-code publish {k_ValidScriptName}")
            .AssertStandardOutputContains(expectedMsg)
            .AssertNoErrors()
            .ExecuteAsync();
    }

    // cloud-code delete tests
    [Test]
    public async Task CloudCodeDeleteThrowsProjectIdNotSetException()
    {
        SetConfigValue("environment-name", CommonKeys.ValidEnvironmentName);

        await new UgsCliTestCase()
            .Command($"cloud-code delete {k_ValidScriptName}")
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardOutputContains(k_ProjectIdNotSetErrorMessage)
            .ExecuteAsync();
    }

    [Test]
    public async Task CloudCodeDeleteThrowsNotLoggedInException()
    {
        SetConfigValue("project-id", CommonKeys.ValidProjectId);
        SetConfigValue("environment-name", CommonKeys.ValidEnvironmentName);

        await new UgsCliTestCase()
            .Command($"cloud-code delete {k_ValidScriptName}")
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardOutputContains(k_LoggedOutErrorMessage)
            .ExecuteAsync();
    }

    [Test]
    public async Task CloudCodeDeleteThrowsInvalidScriptNameException()
    {
        const string invalidScriptName = "test script 123";
        const string expectedMsg = $"{invalidScriptName} is not a valid script name. "
            + "A valid script name must only contain letters, numbers, underscores and dashes.";
        SetConfigValue("project-id", CommonKeys.ValidProjectId);
        SetConfigValue("environment-name", CommonKeys.ValidEnvironmentName);

        await GetLoggedInCli()
            .Command($"cloud-code delete \"{invalidScriptName}\"")
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardOutputContains(expectedMsg)
            .ExecuteAsync();
    }

    [Test]
    public async Task CloudCodeDeleteThrowsEnvironmentIdNotSetException()
    {
        SetConfigValue("project-id", CommonKeys.ValidProjectId);

        await GetLoggedInCli()
            .Command($"cloud-code delete {k_ValidScriptName}")
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardOutputContains(k_EnvironmentNameNotSetErrorMessage)
            .ExecuteAsync();
    }

    [Test]
    public async Task CloudCodeDeleteReturnsZeroExitCode()
    {
        const string expectedMsg = $"Script {k_ValidScriptName} deleted.";
        SetConfigValue("project-id", CommonKeys.ValidProjectId);
        SetConfigValue("environment-name", CommonKeys.ValidEnvironmentName);

        await GetLoggedInCli()
            .Command($"cloud-code delete {k_ValidScriptName}")
            .AssertStandardOutputContains(expectedMsg)
            .AssertNoErrors()
            .ExecuteAsync();
    }

    [TestCase("cloud-code get test-script-123")]
    [TestCase("cloud-code get test-script-123 -j")]
    public async Task CloudCodeGetThrowsProjectIdNotSetException(string command)
    {
        SetConfigValue("environment-name", CommonKeys.ValidEnvironmentName);

        await new UgsCliTestCase()
            .Command(command)
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardOutputContains(k_ProjectIdNotSetErrorMessage)
            .ExecuteAsync();
    }

    [TestCase("cloud-code get test-script-123")]
    [TestCase("cloud-code get test-script-123 -j")]
    public async Task CloudCodeGetThrowsNotLoggedInException(string command)
    {
        SetConfigValue("project-id", CommonKeys.ValidProjectId);
        SetConfigValue("environment-name", CommonKeys.ValidEnvironmentName);

        await new UgsCliTestCase()
            .Command(command)
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardOutputContains(k_LoggedOutErrorMessage)
            .ExecuteAsync();
    }

    [TestCase("cloud-code get \"test script 123\"")]
    [TestCase("cloud-code get \"test script 123\" -j")]
    public async Task CloudCodeGetThrowsInvalidScriptNameException(string command)
    {
        const string expectedMsg = "test script 123 is not a valid script name. "
            + "A valid script name must only contain letters, numbers, underscores and dashes.";
        SetConfigValue("project-id", CommonKeys.ValidProjectId);
        SetConfigValue("environment-name", CommonKeys.ValidEnvironmentName);

        await GetLoggedInCli()
            .Command(command)
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardOutputContains(expectedMsg)
            .ExecuteAsync();
    }

    [Test]
    public async Task CloudCodeGetThrowsEnvironmentIdNotSetException()
    {
        SetConfigValue("project-id", CommonKeys.ValidProjectId);

        await GetLoggedInCli()
            .Command("cloud-code get test-script-123")
            .AssertStandardOutputContains(k_EnvironmentNameNotSetErrorMessage)
            .AssertExitCode(ExitCode.HandledError)
            .ExecuteAsync();
    }

    [Test]
    public async Task CloudCodeGetReturnsZeroExitCode()
    {
        var expectedMsg = $"name: {k_ValidScriptName}{Environment.NewLine}"
            + $"language: JS{Environment.NewLine}type: API{Environment.NewLine}";
        SetConfigValue("project-id", CommonKeys.ValidProjectId);
        SetConfigValue("environment-name", CommonKeys.ValidEnvironmentName);

        await GetLoggedInCli()
            .Command($"cloud-code get {k_ValidScriptName}")
            .AssertStandardOutputContains(expectedMsg)
            .AssertNoErrors()
            .ExecuteAsync();
    }

    [Test]
    public async Task CloudCodeCreateThrowsErrorWhenProjectIdNotSet()
    {
        SetConfigValue("environment-name", CommonKeys.ValidEnvironmentName);

        await GetLoggedInCli()
            .Command($"cloud-code create {k_ValidScriptName} invalidpath")
            .AssertStandardOutputContains(k_ProjectIdNotSetErrorMessage)
            .AssertExitCode(ExitCode.HandledError)
            .ExecuteAsync();
    }

    [Test]
    public async Task CloudCodeCreateThrowsErrorWhenEnvironmentNotSet()
    {
        SetConfigValue("project-id", CommonKeys.ValidProjectId);

        await new UgsCliTestCase()
            .Command($"cloud-code create {k_ValidScriptName} invalidpath")
            .AssertStandardOutputContains(k_EnvironmentNameNotSetErrorMessage)
            .AssertExitCode(ExitCode.HandledError)
            .ExecuteAsync();
    }

    [Test]
    public async Task CloudCodeCreateThrowsErrorWhenScriptTypeIsInvalid()
    {
        const string invalidScriptType = "invalidtype";
        var expectedMsg = $"'{invalidScriptType}' is not a valid {nameof(ScriptType)}."
            + $" Valid {nameof(ScriptType)}: " + string.Join(",", Enum.GetNames<ScriptType>()) + ".";
        SetConfigValue("project-id", CommonKeys.ValidProjectId);
        SetConfigValue("environment-name", CommonKeys.ValidEnvironmentName);

        await GetLoggedInCli()
            .Command($"cloud-code create scriptname invalidpath -t {invalidScriptType}")
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardOutputContains(expectedMsg)
            .ExecuteAsync();
    }

    [Test]
    public async Task CloudCodeCreateThrowsErrorWhenScriptLanguageIsInvalid()
    {
        const string invalidScriptLanguage = "invalidlanguage";
        var expectedMsg = $"'{invalidScriptLanguage}' is not a valid {nameof(Language)}."
            + $" Valid {nameof(Language)}: " + string.Join(",", Enum.GetNames<Language>()) + ".";
        SetConfigValue("project-id", CommonKeys.ValidProjectId);
        SetConfigValue("environment-name", CommonKeys.ValidEnvironmentName);

        await GetLoggedInCli()
            .Command($"cloud-code create scriptname invalidpath -l {invalidScriptLanguage}")
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardOutputContains(expectedMsg)
            .ExecuteAsync();
    }

    [TestCase("\"\"")]
    public async Task CloudCodeCreateThrowsErrorWhenFilepathIsInvalid(string? filepath)
    {
        const string expectedMsg = "The file path provided is null or empty. Please enter a valid file path.";
        SetConfigValue("project-id", CommonKeys.ValidProjectId);
        SetConfigValue("environment-name", CommonKeys.ValidEnvironmentName);

        await GetLoggedInCli()
            .Command($"cloud-code create {k_ValidScriptName} {filepath}")
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardOutputContains(expectedMsg)
            .ExecuteAsync();
    }

    [Test]
    public async Task CloudCodeCreateThrowsErrorWhenFileNotFound()
    {
        const string expectedMsg = "Could not find file";
        SetConfigValue("project-id", CommonKeys.ValidProjectId);
        SetConfigValue("environment-name", CommonKeys.ValidEnvironmentName);

        await GetLoggedInCli()
            .Command($"cloud-code create {k_ValidScriptName} pathToFileThatDoesntExist")
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardOutputContains(expectedMsg)
            .ExecuteAsync();
    }

    [Test]
    public async Task CloudCodeCreateThrowsErrorWhenFileAccessUnauthorized()
    {
        const string unauthorizedFilepath = "Unity.Services.Cli";
        SetConfigValue("project-id", CommonKeys.ValidProjectId);
        SetConfigValue("environment-name", CommonKeys.ValidEnvironmentName);

        await GetLoggedInCli()
            .Command($"cloud-code create {k_ValidScriptName} {unauthorizedFilepath}")
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardOutputContains(k_UnauthorizedFileAccessErrorMessage)
            .ExecuteAsync();
    }

    [TestCase("script.name")]
    [TestCase("script!-name")]
    public async Task CloudCodeCreateThrowsErrorWhenScriptNameInvalid(string scriptName)
    {
        var expectedMsg = $"{scriptName} is not a valid script name. A valid script name must"
            + " only contain letters, numbers, underscores and dashes.";
        const string path = k_ParsingTestFilesDirectory + "/Script.js";
        SetConfigValue("project-id", CommonKeys.ValidProjectId);
        SetConfigValue("environment-name", CommonKeys.ValidEnvironmentName);

        await GetLoggedInCli()
            .Command($"cloud-code create {scriptName} {path}")
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardOutputContains(expectedMsg)
            .ExecuteAsync();
    }

    [Test]
    public async Task CloudCodeCreateThrowsErrorWhenCodeNullOrEmpty()
    {
        const string expectedMsg = "Script could not be created because the code provided is null or empty.";
        SetConfigValue("project-id", CommonKeys.ValidProjectId);
        SetConfigValue("environment-name", CommonKeys.ValidEnvironmentName);
        await File.WriteAllTextAsync(k_ValidFilepath, null);
        var path = Path.GetFullPath(k_ValidFilepath);

        await GetLoggedInCli()
            .Command($"cloud-code create testscript {path}")
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardOutputContains(expectedMsg)
            .ExecuteAsync();
    }

    [Test]
    public async Task CloudCodeCreateThrowsErrorWhenNotLoggedIn()
    {
        const string path = k_ParsingTestFilesDirectory + "/Script.js";
        SetConfigValue("project-id", CommonKeys.ValidProjectId);
        SetConfigValue("environment-name", CommonKeys.ValidEnvironmentName);

        await new UgsCliTestCase()
            .Command($"cloud-code create testscript {path}")
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardOutputContains(k_LoggedOutErrorMessage)
            .ExecuteAsync();
    }

    [Test]
    public async Task CloudCodeCreateThrowsErrorWhenInvalidScriptParameters()
    {
        const string expectedMsg = "Could not convert string to boolean: Yes. Path 'bleu.required'";
        const string path = k_ParsingTestFilesDirectory + "/RequiredInvalidParam.js";
        SetConfigValue("project-id", CommonKeys.ValidProjectId);
        SetConfigValue("environment-name", CommonKeys.ValidEnvironmentName);

        await GetLoggedInCli()
            .Command($"cloud-code create testscript {path}")
            .AssertStandardOutputContains(expectedMsg)
            .ExecuteAsync();
    }

    [Test]
    public async Task CloudCodeCreateParsingThrowsErrorOnNonPrimitiveTypes()
    {
        const string expectedMsg = "Do not know how to serialize a BigInt";
        const string path = k_ParsingTestFilesDirectory + "/BigInt.js";
        SetConfigValue("project-id", CommonKeys.ValidProjectId);
        SetConfigValue("environment-name", CommonKeys.ValidEnvironmentName);

        await GetLoggedInCli()
            .Command($"cloud-code create testscript {path}")
            .AssertStandardOutputContains(expectedMsg)
            .ExecuteAsync();
    }

    [Test]
    public async Task CloudCodeCreateParsingThrowsErrorOnCyclicReference()
    {
        const string expectedMsg = "Converting circular structure to JSON";
        const string path = k_ParsingTestFilesDirectory + "/CyclicReference.js";
        SetConfigValue("project-id", CommonKeys.ValidProjectId);
        SetConfigValue("environment-name", CommonKeys.ValidEnvironmentName);

        await GetLoggedInCli()
            .Command($"cloud-code create testscript {path}")
            .AssertStandardOutputContains(expectedMsg)
            .ExecuteAsync();
    }

    [Test]
    public async Task CloudCodeCreateParsingThrowsErrorOnInfiniteLoop()
    {
        const string expectedMsg = "The in-script parameter parsing is taking too long";
        const string path = k_ParsingTestFilesDirectory + "/InfiniteLoop.js";
        SetConfigValue("project-id", CommonKeys.ValidProjectId);
        SetConfigValue("environment-name", CommonKeys.ValidEnvironmentName);

        await GetLoggedInCli()
            .Command($"cloud-code create testscript {path}")
            .AssertStandardOutputContains(expectedMsg)
            .ExecuteAsync();
    }

    [Test]
    [Ignore($"This test is unstable on Windows. " +
        $"We'll keep it ignored until we find the cause, it's already tested on unit tests anyway.")]
    public async Task CloudCodeCreateParsingThrowsErrorOnMemoryAllocationOverload()
    {
        const string expectedMsg = "JavaScript heap out of memory";
        const string path = k_ParsingTestFilesDirectory + "/MemoryAllocation.js";
        SetConfigValue("project-id", CommonKeys.ValidProjectId);
        SetConfigValue("environment-name", CommonKeys.ValidEnvironmentName);

        await GetLoggedInCli()
            .Command($"cloud-code create testscript {path}")
            .AssertStandardOutputContains(expectedMsg)
            .ExecuteAsync();
    }

    [Test]
    public async Task CloudCodeCreateParsingThrowsErrorOnIOAccess()
    {
        const string expectedMsg = "\"required\" resource might be used during script parsing";
        const string path = k_ParsingTestFilesDirectory + "/ReadFile.js";
        SetConfigValue("project-id", CommonKeys.ValidProjectId);
        SetConfigValue("environment-name", CommonKeys.ValidEnvironmentName);

        await GetLoggedInCli()
            .Command($"cloud-code create testscript {path}")
            .AssertStandardOutputContains(expectedMsg)
            .ExecuteAsync();
    }

    [Test]
    public async Task CloudCodeCreateZeroExitCodeWithParamParsing()
    {
        const string path = k_ParsingTestFilesDirectory + "/Script.js";
        SetConfigValue("project-id", CommonKeys.ValidProjectId);
        SetConfigValue("environment-name", CommonKeys.ValidEnvironmentName);

        await GetLoggedInCli()
            .Command($"cloud-code create {k_ValidScriptName} {path}")
            .AssertStandardOutputContains($"Script '{k_ValidScriptName}' created.")
            .ExecuteAsync();
    }

    [Test]
    public async Task CloudCodeCreateZeroExitCodeWithNoParamParsing()
    {
        const string path = k_ParsingTestFilesDirectory + "/NoParameter.js";
        SetConfigValue("project-id", CommonKeys.ValidProjectId);
        SetConfigValue("environment-name", CommonKeys.ValidEnvironmentName);

        await GetLoggedInCli()
            .Command($"cloud-code create {k_ValidScriptName} {path}")
            .AssertStandardOutputContains($"Script '{k_ValidScriptName}' created.")
            .ExecuteAsync();
    }

    [Test]
    public async Task CloudCodeUpdateThrowsErrorWhenProjectIdNotSet()
    {
        SetConfigValue("environment-name", CommonKeys.ValidEnvironmentName);

        await new UgsCliTestCase()
            .Command($"cloud-code update {k_ValidScriptName} invalidpath")
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardOutputContains(k_ProjectIdNotSetErrorMessage)
            .ExecuteAsync();
    }

    [Test]
    public async Task CloudCodeUpdateThrowsErrorWhenEnvironmentNotSet()
    {
        SetConfigValue("project-id", CommonKeys.ValidProjectId);

        await new UgsCliTestCase()
            .Command($"cloud-code update {k_ValidScriptName} invalidpath")
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardOutputContains(k_EnvironmentNameNotSetErrorMessage)
            .ExecuteAsync();
    }

    [TestCase("\"\"")]
    public async Task CloudCodeUpdateThrowsErrorWhenFilepathIsInvalid(string? filepath)
    {
        const string expectedMsg = "The file path provided is null or empty. Please enter a valid file path.";
        SetConfigValue("project-id", CommonKeys.ValidProjectId);
        SetConfigValue("environment-name", CommonKeys.ValidEnvironmentName);

        await GetLoggedInCli()
            .Command($"cloud-code update {k_ValidScriptName} {filepath}")
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardOutputContains(expectedMsg)
            .ExecuteAsync();
    }

    [Test]
    public async Task CloudCodeUpdateThrowsErrorWhenFileNotFound()
    {
        const string expectedMsg = "Could not find file";
        SetConfigValue("project-id", CommonKeys.ValidProjectId);
        SetConfigValue("environment-name", CommonKeys.ValidEnvironmentName);

        await GetLoggedInCli()
            .Command("cloud-code update scriptname pathToFileThatDoesntExist")
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardOutputContains(expectedMsg)
            .ExecuteAsync();
    }

    [Test]
    public async Task CloudCodeUpdateThrowsErrorWhenFileAccessUnauthorized()
    {
        const string unauthorizedFilepath = "Unity.Services.Cli";
        SetConfigValue("project-id", CommonKeys.ValidProjectId);
        SetConfigValue("environment-name", CommonKeys.ValidEnvironmentName);

        await GetLoggedInCli()
            .Command($"cloud-code update scriptname {unauthorizedFilepath}")
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardOutputContains(k_UnauthorizedFileAccessErrorMessage)
            .ExecuteAsync();
    }

    [TestCase("script.name")]
    [TestCase("script!-name")]
    public async Task CloudCodeUpdateThrowsErrorWhenScriptNameInvalid(string scriptName)
    {
        var expectedMsg = $"{scriptName} is not a valid script name. A valid script name must"
            + " only contain letters, numbers, underscores and dashes.";
        const string path = k_ParsingTestFilesDirectory + "/Script.js";
        SetConfigValue("project-id", CommonKeys.ValidProjectId);
        SetConfigValue("environment-name", CommonKeys.ValidEnvironmentName);

        await GetLoggedInCli()
            .Command($"cloud-code update {scriptName} {path}")
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardOutputContains(expectedMsg)
            .ExecuteAsync();
    }

    [Test]
    public async Task CloudCodeUpdateThrowsErrorWhenCodeNullOrEmpty()
    {
        const string expectedMsg = "Script could not be updated because the code provided is null or empty.";
        SetConfigValue("project-id", CommonKeys.ValidProjectId);
        SetConfigValue("environment-name", CommonKeys.ValidEnvironmentName);
        await File.WriteAllTextAsync(k_ValidFilepath, null);
        var path = Path.GetFullPath(k_ValidFilepath);

        await GetLoggedInCli()
            .Command($"cloud-code update testscript {path}")
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardOutputContains(expectedMsg)
            .ExecuteAsync();
    }

    [Test]
    public async Task CloudCodeUpdateThrowsErrorWhenNotLoggedIn()
    {
        const string path = k_ParsingTestFilesDirectory + "/Script.js";
        SetConfigValue("project-id", CommonKeys.ValidProjectId);
        SetConfigValue("environment-name", CommonKeys.ValidEnvironmentName);

        await new UgsCliTestCase()
            .Command($"cloud-code update testscript {path}")
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardOutputContains(k_LoggedOutErrorMessage)
            .ExecuteAsync();
    }

    [Test]
    public async Task CloudCodeUpdateThrowsErrorWhenInvalidScriptParameters()
    {
        const string expectedMsg = "Could not convert string to boolean: Yes.";
        const string path = k_ParsingTestFilesDirectory + "/RequiredInvalidParam.js";
        SetConfigValue("project-id", CommonKeys.ValidProjectId);
        SetConfigValue("environment-name", CommonKeys.ValidEnvironmentName);

        await GetLoggedInCli()
            .Command($"cloud-code update {k_ValidScriptName} {path}")
            .AssertStandardOutputContains(expectedMsg)
            .ExecuteAsync();
    }

    [Test]
    public async Task CloudCodeUpdateZeroExitCodeWithParamParsing()
    {
        const string expectedMsg = $"Script '{k_ValidScriptName}' updated.";
        const string path = k_ParsingTestFilesDirectory + "/Script.js";
        SetConfigValue("project-id", CommonKeys.ValidProjectId);
        SetConfigValue("environment-name", CommonKeys.ValidEnvironmentName);

        await GetLoggedInCli()
            .Command($"cloud-code update {k_ValidScriptName} {path}")
            .AssertStandardOutputContains(expectedMsg)
            .AssertNoErrors()
            .ExecuteAsync();
    }

    [Test]
    public async Task CloudCodeUpdateZeroExitCodeWithNoParamParsing()
    {
        const string expectedMsg = $"Script '{k_ValidScriptName}' updated.";
        const string path = k_ParsingTestFilesDirectory + "/NoParameter.js";
        SetConfigValue("project-id", CommonKeys.ValidProjectId);
        SetConfigValue("environment-name", CommonKeys.ValidEnvironmentName);

        await GetLoggedInCli()
            .Command($"cloud-code update {k_ValidScriptName} {path}")
            .AssertNoErrors()
            .AssertStandardOutputContains(expectedMsg)
            .ExecuteAsync();
    }
}
