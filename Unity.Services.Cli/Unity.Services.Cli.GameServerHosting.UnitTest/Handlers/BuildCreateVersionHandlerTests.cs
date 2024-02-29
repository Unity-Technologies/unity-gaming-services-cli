using System.ComponentModel;
using Moq;
using Spectre.Console;
using Unity.Services.Cli.Common.Console;
using Unity.Services.Cli.Common.Exceptions;
using Unity.Services.Cli.GameServerHosting.Handlers;
using Unity.Services.Cli.GameServerHosting.Input;
using Unity.Services.Gateway.GameServerHostingApiV1.Generated.Model;

namespace Unity.Services.Cli.GameServerHosting.UnitTest.Handlers;

[TestFixture]
partial class BuildCreateVersionHandlerTests : HandlerCommon
{
    [SetUp]
    public new void SetUp()
    {
        base.SetUp();

        SetUpTempFiles();
    }

    [Test]
    public async Task BuildCreateVersionAsync_CallsLoadingIndicatorStartLoading()
    {
        var mockLoadingIndicator = new Mock<ILoadingIndicator>();

        await BuildCreateVersionHandler.BuildCreateVersionAsync(
            null!,
            MockUnityEnvironment.Object,
            null!,
            null!,
            MockHttpClient!.Object,
            mockLoadingIndicator.Object,
            CancellationToken.None
        );

        mockLoadingIndicator.Verify(
            ex => ex
                .StartLoadingAsync(It.IsAny<string>(), It.IsAny<Func<StatusContext?, Task>>()),
            Times.Once);
    }

    [Test]
    public async Task BuildCreateVersionAsync_CallsFetchIdentifierAsync()
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
            CancellationToken.None);

        MockUnityEnvironment.Verify(ex => ex.FetchIdentifierAsync(CancellationToken.None), Times.Once);
    }

    [TestCase(CreateBuildRequest.BuildTypeEnum.CONTAINER)]
    [TestCase(CreateBuildRequest.BuildTypeEnum.S3)]
    [TestCase(CreateBuildRequest.BuildTypeEnum.FILEUPLOAD)]
    public Task BuildCreateVersionAsync_InvalidBuildVersionName(CreateBuildRequest.BuildTypeEnum buildType)
    {
        BuildCreateVersionInput input = buildType switch
        {
            CreateBuildRequest.BuildTypeEnum.CONTAINER => new()
            {
                CloudProjectId = ValidProjectId,
                TargetEnvironmentName = ValidEnvironmentName,
                BuildId = ValidBuildIdContainer.ToString(),
                ContainerTag = ValidContainerTag,
                BuildVersionName = InValidBuildVersionName
            },
            CreateBuildRequest.BuildTypeEnum.S3 => new()
            {
                CloudProjectId = ValidProjectId,
                TargetEnvironmentName = ValidEnvironmentName,
                BuildId = ValidBuildIdBucket.ToString(),
                BuildVersionName = InValidBuildVersionName,
                AccessKey = "accessKey",
                BucketUrl = "bucketUrl",
                SecretKey = "secretKey"
            },
            CreateBuildRequest.BuildTypeEnum.FILEUPLOAD => new()
            {
                CloudProjectId = ValidProjectId,
                TargetEnvironmentName = ValidEnvironmentName,
                BuildId = BuildWithOneFileId.ToString(),
                BuildVersionName = InValidBuildVersionName,
                FileDirectory = m_TempDirectory
            },
            _ => throw new InvalidEnumArgumentException()
        };

        Assert.ThrowsAsync<CliException>(
            async () => await BuildCreateVersionHandler.BuildCreateVersionAsync(
                input,
                MockUnityEnvironment.Object,
                GameServerHostingService!,
                MockLogger!.Object,
                MockHttpClient!.Object,
                CancellationToken.None));

        return Task.CompletedTask;
    }
}
