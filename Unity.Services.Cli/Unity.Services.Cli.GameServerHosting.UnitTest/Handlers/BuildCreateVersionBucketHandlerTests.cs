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
    [TestCase(null, "secretKey", "bucketUrl")]
    [TestCase("accessKey", null, "bucketUrl")]
    [TestCase("accessKey", "secretKey", null)]
    public void BuildCreateVersionAsync_Bucket_MissingInputThrowsException(
        string? accessKey,
        string? secretKey,
        string? bucketUrl
    )
    {
        BuildCreateVersionInput input = new()
        {
            CloudProjectId = ValidProjectId,
            TargetEnvironmentName = ValidEnvironmentName,
            BuildId = ValidBuildIdBucket.ToString(),
            AccessKey = accessKey,
            SecretKey = secretKey,
            BucketUrl = bucketUrl
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
                ValidBuildIdBucket,
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

    [Test]
    public async Task BuildCreateAsync_Bucket_CallsGetService()
    {
        BuildCreateVersionInput input = new()
        {
            CloudProjectId = ValidProjectId,
            TargetEnvironmentName = ValidEnvironmentName,
            BuildId = ValidBuildIdBucket.ToString(),
            AccessKey = "accessKey",
            BucketUrl = "bucketUrl",
            SecretKey = "secretKey"
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
                new Guid(input.CloudProjectId),
                new Guid(ValidEnvironmentId),
                ValidBuildIdBucket,
                It.IsAny<CreateNewBuildVersionRequest>(),
                0,
                CancellationToken.None
            ),
            Times.Once);
    }

    [TestCase(
        null,
        "s3://bucket.url",
        "secretKey",
        null,
        null,
        typeof(MissingInputException),
        TestName = "Missing access key"
    )]
    [TestCase(
        "accessKey",
        null,
        "secretKey",
        null,
        null,
        typeof(MissingInputException),
        TestName = "Missing bucket url"
    )]
    [TestCase(
        "accessKey",
        "s3://bucket.url",
        null,
        null,
        null,
        typeof(MissingInputException),
        TestName = "Missing secret key"
    )]
    [TestCase(
        "accessKey",
        "s3://bucket.url",
        "secretKey",
        "v1",
        null,
        typeof(CliException),
        TestName = "Container tag with S3 build"
    )]
    [TestCase(
        "accessKey",
        "s3://bucket.url",
        "secretKey",
        null,
        "/path/to/files",
        typeof(CliException),
        TestName = "File directory with S3 build"
    )]
    public void ValidateInput_Bucket(
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

        Assert.Throws(expectedExceptionType, () => BuildCreateVersionHandler.ValidateBucketInput(input));
    }
}
