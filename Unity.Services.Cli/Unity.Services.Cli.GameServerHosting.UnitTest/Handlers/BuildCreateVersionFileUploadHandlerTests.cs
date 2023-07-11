using System.Net;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
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
    string? m_TempDirectory;
    string? m_TempDirectoryEmpty;
    string? m_TempFilePath;

    void SetUpTempFiles()
    {
        m_TempDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(m_TempDirectory);

        if (BuildsApi == null) throw new Exception("static BuildsAPI not initialised");

        // write a file to the temp directory
        m_TempFilePath = Path.Combine(m_TempDirectory, BuildWithOneFileFileName);
        File.WriteAllText(m_TempFilePath, "file content to upload");

        // emptyDirectory
        m_TempDirectoryEmpty = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
    }

    [TestCase(null, "/path/to/files", false)]
    [TestCase(ValidBuildIdContainer, null, false)]
    [TestCase(ValidBuildIdContainer, "/path/to/files", null)]
    public void BuildCreateVersionAsync_FileUpload_MissingInputThrowsException(
        long? buildId,
        string? directory,
        bool? removeOldFiles
    )
    {
        BuildCreateVersionInput input = new()
        {
            CloudProjectId = ValidProjectId,
            TargetEnvironmentName = ValidEnvironmentName,
            BuildId = buildId.HasValue ? buildId.ToString() : null,
            FileDirectory = directory,
            RemoveOldFiles = removeOldFiles
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
        BuildWithOneFileId,
        1,
        false,
        100)]
    [TestCase(
        BuildWithTwoFilesId,
        1,
        true,
        100)]
    [TestCase(
        BuildWithTwoFilesId,
        1,
        false,
        1)]
    [TestCase(
        BuildWithOneFileId,
        1,
        false,
        2)]
    public async Task BuildCreateVersionAsync_FileUpload_HandlesAFile(
        long buildId,
        int expectedSignedUrUploadsRequests,
        bool removeOldFiles,
        int limit
    )
    {
        var mockMessageHandler = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        mockMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Returns(
                (HttpRequestMessage _, CancellationToken _) =>
                {
                    // always return a 200 OK response
                    var response = new HttpResponseMessage(HttpStatusCode.OK);

                    return Task.FromResult(response);
                })
            .Verifiable();

        BuildCreateVersionInput input = new()
        {
            CloudProjectId = ValidProjectId,
            TargetEnvironmentName = ValidEnvironmentName,
            BuildId = buildId.ToString(),
            FileDirectory = m_TempDirectory,
            RemoveOldFiles = removeOldFiles
        };

        await BuildCreateVersionHandler.BuildCreateVersionAsync(
            input,
            MockUnityEnvironment.Object,
            GameServerHostingService!,
            MockLogger!.Object,
            new HttpClient(mockMessageHandler.Object),
            CancellationToken.None);

        mockMessageHandler.Protected()
            .Verify(
                "SendAsync",
                Times.Exactly(expectedSignedUrUploadsRequests),
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>());

        BuildsApi!.DefaultBuildsClient.Verify(
            api => api.CreateNewBuildVersionAsync(
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<long>(),
                It.IsAny<CreateNewBuildVersionRequest>(),
                0,
                CancellationToken.None
            ),
            Times.Once);
    }

    [Test]
    public async Task BuildCreateVersionAsync_FileUpload_EmptyDirectory()
    {
        BuildCreateVersionInput input = new()
        {
            CloudProjectId = ValidProjectId,
            TargetEnvironmentName = ValidEnvironmentName,
            BuildId = BuildWithOneFileId.ToString(),
            FileDirectory = m_TempDirectoryEmpty,
            RemoveOldFiles = false
        };

        await BuildCreateVersionHandler.BuildCreateVersionAsync(
            input,
            MockUnityEnvironment.Object,
            GameServerHostingService!,
            MockLogger!.Object,
            MockHttpClient!.Object,
            CancellationToken.None
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
    }

    [TestCase(
        null,
        null,
        null,
        null,
        null,
        typeof(MissingInputException),
        TestName = "Missing file directory"
    )]
    [TestCase(
        null,
        "s3://bucket.url",
        null,
        null,
        "/path/to/files",
        typeof(CliException),
        TestName = "S3 bucket with file build"
    )]
    [TestCase(
        null,
        null,
        null,
        "v1",
        "/path/to/files",
        typeof(CliException),
        TestName = "Container tag with file build"
    )]
    public void ValidateInput_FileUpload(
        string? accessKey,
        string? bucketUrl,
        string? secretKey,
        string? containerTag,
        string? fileDirectory,
        Type expectedExceptionType
    )
    {
        var input = new BuildCreateVersionInput
        {
            CloudProjectId = ValidProjectId,
            TargetEnvironmentName = ValidEnvironmentName,
            AccessKey = accessKey,
            BucketUrl = bucketUrl,
            SecretKey = secretKey,
            ContainerTag = containerTag,
            FileDirectory = fileDirectory,
        };

        Assert.Throws(expectedExceptionType, () => BuildCreateVersionHandler.ValidateFileUploadInput(input));
    }

    [Test]
    public void BuildCreateVersionAsync_FileUpload_SyncingBuildThrowsException()
    {
        BuildCreateVersionInput input = new()
        {
            CloudProjectId = ValidProjectId,
            TargetEnvironmentName = ValidEnvironmentName,
            BuildId = SyncingBuildId.ToString(),
            FileDirectory = m_TempDirectory,
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
}
