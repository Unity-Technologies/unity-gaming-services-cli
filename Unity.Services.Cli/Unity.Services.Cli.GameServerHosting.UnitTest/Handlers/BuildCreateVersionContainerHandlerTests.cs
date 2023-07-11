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
    [Test]
    public void BuildCreateVersionAsync_Container_MissingInputThrowsException()
    {
        BuildCreateVersionInput input = new()
        {
            CloudProjectId = ValidProjectId,
            TargetEnvironmentName = ValidEnvironmentName,
            BuildId = ValidBuildIdContainer.ToString(),
            ContainerTag = null
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

    [Test]
    public async Task BuildCreateAsync_Container_CallsGetService()
    {
        BuildCreateVersionInput input = new()
        {
            CloudProjectId = ValidProjectId,
            TargetEnvironmentName = ValidEnvironmentName,
            BuildId = ValidBuildIdContainer.ToString(),
            ContainerTag = ValidContainerTag
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
                ValidBuildIdContainer,
                It.IsAny<CreateNewBuildVersionRequest>(),
                0,
                CancellationToken.None
            ),
            Times.Once);
    }

    [TestCase(
        null,
        null,
        null,
        null,
        null,
        typeof(MissingInputException),
        TestName = "Missing container tag"
    )]
    [TestCase(
        null,
        "s3://bucket.url",
        null,
        "v1",
        null,
        typeof(CliException),
        TestName = "S3 bucket with container build"
    )]
    [TestCase(
        null,
        null,
        null,
        "v1",
        "/path/to/files",
        typeof(CliException),
        TestName = "File directory with container build"
    )]
    public void ValidateInput_Container(
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

        Assert.Throws(expectedExceptionType, () => BuildCreateVersionHandler.ValidateContainerInput(input));
    }
}
