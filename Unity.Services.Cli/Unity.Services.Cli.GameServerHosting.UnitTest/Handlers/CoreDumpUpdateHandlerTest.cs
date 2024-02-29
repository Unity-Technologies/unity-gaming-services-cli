using System.IO.Abstractions;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json;
using Unity.Services.Cli.Common.Exceptions;
using Unity.Services.Cli.Common.Logging;
using Unity.Services.Cli.GameServerHosting.Handlers;
using Unity.Services.Cli.GameServerHosting.Input;
using Unity.Services.Cli.GameServerHosting.Model;
using Unity.Services.Cli.GameServerHosting.Services;
using Unity.Services.Cli.TestUtils;

namespace Unity.Services.Cli.GameServerHosting.UnitTest.Handlers;

[TestFixture]
[TestOf(typeof(CoreDumpUpdateHandler))]
class CoreDumpUpdateHandlerTest : HandlerCommon
{
    static IEnumerable<TestCaseData> TestCases()
    {
        var validGcsCredentials = new GcsCredentials(CoreDumpMockValidAccessId, CoreDumpMockValidPrivateKey);
        var invalidGcsCredentials = new GcsCredentials("invalid access id", CoreDumpMockValidPrivateKey);
        yield return new TestCaseData(
            ValidProjectId,
            ValidEnvironmentName,
            CoreDumpMockFleetIdWithEnabledConfig,
            CoreDumpStateConverter.StateEnum.Enabled.ToString(),
            CoreDumpMockValidCredentialsFilePath,
            CoreDumpMockValidBucketName,
            validGcsCredentials,
            $"fleetId: {CoreDumpMockFleetIdWithEnabledConfig}",
            false,
            ""
        ).SetName("Core Dump Config should be created");
        yield return new TestCaseData(
            ValidProjectId,
            ValidEnvironmentName,
            CoreDumpMockFleetIdWithoutConfig,
            CoreDumpStateConverter.StateEnum.Enabled.ToString(),
            CoreDumpMockValidCredentialsFilePath,
            CoreDumpMockValidBucketName,
            validGcsCredentials,
            "",
            true,
            "Core dump config does not exists"
        ).SetName("Core Dump Config already does not exists");
        yield return new TestCaseData(
            ValidProjectId,
            ValidEnvironmentName,
            CoreDumpMockFleetIdWithEnabledConfig,
            CoreDumpStateConverter.StateEnum.Enabled.ToString(),
            CoreDumpMockValidCredentialsFilePath,
            CoreDumpMockValidBucketName,
            invalidGcsCredentials,
            "",
            true,
            "Invalid credentials"
        ).SetName("Core Dump Config invalid credentials");
        yield return new TestCaseData(
            ValidProjectId,
            ValidEnvironmentName,
            CoreDumpMockFleetIdWithEnabledConfig,
            CoreDumpStateConverter.StateEnum.Enabled.ToString(),
            CoreDumpMockValidCredentialsFilePath,
            CoreDumpMockValidBucketName,
            validGcsCredentials,
            "",
            true,
            "Invalid GCS credentials file format"
        ).SetName("Invalid file format");
        yield return new TestCaseData(
            ValidProjectId,
            ValidEnvironmentName,
            null,
            CoreDumpStateConverter.StateEnum.Enabled.ToString(),
            CoreDumpMockValidCredentialsFilePath,
            CoreDumpMockValidBucketName,
            invalidGcsCredentials,
            $"fleetId: {CoreDumpMockFleetIdWithEnabledConfig}",
            true,
            "Missing value for input: 'fleet-id'"
        ).SetName("Core Dump Config should be created");
    }

    [TestCaseSource(nameof(TestCases))]
    public async Task CoreDumpCreateAsync(
        string projectId,
        string environmentName,
        string fleetId,
        string state,
        string credentialsFile,
        string buketName,
        GcsCredentials gcsCredentials,
        string outputContains,
        bool expectedException = false,
        string exceptionContains = "")
    {
        var fileMoq = new Mock<IFile>();
        fileMoq.Setup(f => f.Exists(It.IsAny<string>()))
            .Returns(true);
        fileMoq.Setup(f => f.ReadAllText(It.IsAny<string>()))
            .Returns(JsonConvert.SerializeObject(gcsCredentials));

        CoreDumpUpdateInput input = new()
        {
            CloudProjectId = projectId,
            TargetEnvironmentName = environmentName,
            FleetId = fleetId,
            CredentialsFile = credentialsFile,
            State = state,
            GcsBucket = buketName,
        };

        try
        {
            await CoreDumpUpdateHandler.CoreDumpUpdateAsync(
                input,
                MockUnityEnvironment.Object,
                GameServerHostingService!,
                MockLogger!.Object,
                new GcsCredentialParser(fileMoq.Object),
                CancellationToken.None
            );
        }
        catch (CliException e) when (expectedException)
        {
            Assert.That(e.Message, Does.Contain(exceptionContains));
            return;
        }

        TestsHelper.VerifyLoggerWasCalled(
            MockLogger,
            LogLevel.Critical,
            LoggerExtension.ResultEventId,
            Times.Once,
            outputContains);
    }
}
