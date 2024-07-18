using System.IO.Abstractions;
using Microsoft.Extensions.Logging;
using Moq;
using Unity.Services.Cli.Common.Exceptions;
using Unity.Services.Cli.Common.Logging;
using Unity.Services.Cli.GameServerHosting.Exceptions;
using Unity.Services.Cli.GameServerHosting.Handlers;
using Unity.Services.Cli.GameServerHosting.Input;
using Unity.Services.Cli.TestUtils;
using Unity.Services.Gateway.GameServerHostingApiV1.Generated.Model;

namespace Unity.Services.Cli.GameServerHosting.UnitTest.Handlers;

partial class BuildCreateVersionHandlerTests
{
    [TestCase(null, "./test/sa.json", "gs://bucket/url")]
    [TestCase(ValidBuildIdGcs, null, "gs://bucket/url")]
    [TestCase(ValidBuildIdGcs, "./test/sa.json", null)]
    public void BuildCreateVersionAsync_GCS_MissingInputThrowsException(
        long? buildId,
        string? serviceAccountJsonFile,
        string? bucketUrl
    )
    {
        BuildCreateVersionInput input = new()
        {
            CloudProjectId = ValidProjectId,
            TargetEnvironmentName = ValidEnvironmentName,
            BuildId = buildId.HasValue ? buildId.ToString() : null,
            ServiceAccountJsonFile = serviceAccountJsonFile,
            BucketUrl = bucketUrl,
            BuildVersionName = ValidBuildVersionName,
        };

        Assert.ThrowsAsync<MissingInputException>(
            () => BuildCreateVersionHandler.BuildCreateVersionAsync(
                input,
                MockUnityEnvironment.Object,
                GameServerHostingService!,
                MockLogger!.Object,
                new Mock<HttpClient>().Object,
                CancellationToken.None
            )
        );

        BuildsApi!.DefaultBuildsClient.Verify(
            api => api.CreateNewBuildVersionAsync(
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<long>(),
                It.IsAny<CreateNewBuildVersionRequest>(),
                0,
                CancellationToken.None
            ),
            Times.Never);

        TestsHelper.VerifyLoggerWasCalled(
            MockLogger!,
            LogLevel.Critical,
            LoggerExtension.ResultEventId,
            Times.Never);
    }

    [TestCase(
        "secretKey",
        null,
        null,
        null,
        TestName = "Redundant AccessKey")]
    [TestCase(
        null,
        "accessKey",
        null,
        null,
        TestName = "Redundant SecretKey")]
    [TestCase(
        null,
        null,
        "containerTag",
        null,
        TestName = "Redundant fileDirectory")]
    [TestCase(
        null,
        null,
        null,
        "fileDirectory",
        TestName = "Redundant containerTag")]
    public void BuildCreateVersionAsync_GCS_UnknownInputThrowsException(
        string? secretKey = null,
        string? accessKey = null,
        string? containerTag = null,
        string? fileDirectory = null
    )
    {
        BuildCreateVersionInput input = new()
        {
            CloudProjectId = ValidProjectId,
            TargetEnvironmentName = ValidEnvironmentName,
            BuildId = ValidBuildIdGcs.ToString(),
            ServiceAccountJsonFile = "./test/sa.json",
            BucketUrl = "gs://bucket/url",
            BuildVersionName = ValidBuildVersionName,
            SecretKey = secretKey,
            AccessKey = accessKey,
            ContainerTag = containerTag,
            FileDirectory = fileDirectory
        };

        Assert.ThrowsAsync<CliException>(
            () => BuildCreateVersionHandler.BuildCreateVersionAsync(
                input,
                MockUnityEnvironment.Object,
                GameServerHostingService!,
                MockLogger!.Object,
                new Mock<HttpClient>().Object,
                CancellationToken.None
            )
        );

        BuildsApi!.DefaultBuildsClient.Verify(
            api => api.CreateNewBuildVersionAsync(
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<long>(),
                It.IsAny<CreateNewBuildVersionRequest>(),
                0,
                CancellationToken.None
            ),
            Times.Never);

        TestsHelper.VerifyLoggerWasCalled(
            MockLogger!,
            LogLevel.Critical,
            LoggerExtension.ResultEventId,
            Times.Never);
    }


    [TestCase(
        false,
        true,
        false,
        TestName = "golden path")]
    [TestCase(
        true,
        false,
        false,
        TestName = "io exception")]
    [TestCase(
        false,
        true,
        true,
        TestName = "api exception")]
    public void BuildCreateVersionAsync_GCS(
        bool throwIoException,
        bool expectApiCall,
        bool throwApiException
    )
    {
        var serviceAcountPath = "./tmp/sa.json";
        var fullServiceAccountPath = Path.GetFullPath(serviceAcountPath);

        BuildCreateVersionInput input = new()
        {
            CloudProjectId = ValidProjectId,
            TargetEnvironmentName = ValidEnvironmentName,
            BuildId = ValidBuildIdGcs.ToString(),
            BucketUrl = "gc://bucket/url",
            ServiceAccountJsonFile = serviceAcountPath,
            BuildVersionName = throwApiException ? InValidBuildVersionName : ValidBuildVersionName
        };

        var fileMock = new Mock<IFile>();
        var expectation = fileMock.Setup(file => file.ReadAllTextAsync(fullServiceAccountPath, CancellationToken.None));

        if (throwIoException)
        {
            expectation.ThrowsAsync(new IOException("test"));
        }
        else
        {
            expectation.ReturnsAsync("{}");
        }
        BuildCreateVersionHandler.FileSystem = fileMock.Object;


        if (throwIoException || throwApiException)
        {
            Assert.ThrowsAsync<CliException>(FuncCall);
        }
        else
        {
            Assert.DoesNotThrowAsync(FuncCall);
        }


        BuildsApi!.DefaultBuildsClient.Verify(
            api => api.CreateNewBuildVersionAsync(
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<long>(),
                It.IsAny<CreateNewBuildVersionRequest>(),
                0,
                CancellationToken.None
            ),
            expectApiCall ? Times.Once : Times.Never);

        TestsHelper.VerifyLoggerWasCalled(
            MockLogger!,
            LogLevel.Critical,
            LoggerExtension.ResultEventId,
            Times.Never);
        return;

        Task FuncCall() =>
            BuildCreateVersionHandler.BuildCreateVersionAsync(
                input,
                MockUnityEnvironment.Object,
                GameServerHostingService!,
                MockLogger!.Object,
                new Mock<HttpClient>().Object,
                CancellationToken.None);
    }
}
