using Microsoft.Extensions.Logging;
using Moq;
using Spectre.Console;
using Unity.Services.Cli.Common.Console;
using Unity.Services.Cli.Common.Exceptions;
using Unity.Services.Cli.Common.Logging;
using Unity.Services.Cli.GameServerHosting.Exceptions;
using Unity.Services.Cli.GameServerHosting.Handlers;
using Unity.Services.Cli.GameServerHosting.Input;
using Unity.Services.Cli.TestUtils;
using Unity.Services.Gateway.GameServerHostingApiV1.Generated.Client;
using Unity.Services.Gateway.GameServerHostingApiV1.Generated.Model;
using static Unity.Services.Gateway.GameServerHostingApiV1.Generated.Model.CreateBuildRequest;

namespace Unity.Services.Cli.GameServerHosting.UnitTest.Handlers;

[TestFixture]
class BuildCreateHandlerTests : HandlerCommon
{
    [Test]
    public async Task BuildCreateAsync_CallsLoadingIndicatorStartLoading()
    {
        var mockLoadingIndicator = new Mock<ILoadingIndicator>();

        await BuildCreateHandler.BuildCreateAsync(
            null!,
            MockUnityEnvironment.Object,
            null!,
            null!,
            mockLoadingIndicator.Object,
            CancellationToken.None);

        mockLoadingIndicator.Verify(
            ex => ex
                .StartLoadingAsync(It.IsAny<string>(), It.IsAny<Func<StatusContext?, Task>>()),
            Times.Once);
    }

    [Test]
    public async Task BuildCreateAsync_CallsFetchIdentifierAsync()
    {
        BuildCreateInput input = new()
        {
            CloudProjectId = ValidProjectId,
            TargetEnvironmentName = ValidEnvironmentName,
            BuildName = "Build One",
            BuildOsFamily = OsFamilyEnum.LINUX,
            BuildType = BuildTypeEnum.FILEUPLOAD
        };

        await BuildCreateHandler.BuildCreateAsync(
            input,
            MockUnityEnvironment.Object,
            GameServerHostingService!,
            MockLogger!.Object,
            CancellationToken.None
        );

        MockUnityEnvironment.Verify(ex => ex.FetchIdentifierAsync(CancellationToken.None), Times.Once);
    }

    [TestCase(
        ValidProjectId,
        ValidEnvironmentName,
        null,
        OsFamilyEnum.LINUX,
        BuildTypeEnum.FILEUPLOAD)]
    [TestCase(
        ValidProjectId,
        ValidEnvironmentName,
        null,
        OsFamilyEnum.LINUX,
        BuildTypeEnum.CONTAINER)]
    [TestCase(
        ValidProjectId,
        ValidEnvironmentName,
        null,
        OsFamilyEnum.LINUX,
        BuildTypeEnum.S3)]
    public void BuildCreateAsync_NullBuildNameThrowsException(
        string projectId,
        string environmentName,
        string buildName,
        OsFamilyEnum osFamily,
        BuildTypeEnum buildType
    )
    {
        BuildCreateInput input = new()
        {
            CloudProjectId = projectId,
            TargetEnvironmentName = environmentName,
            BuildName = buildName,
            BuildOsFamily = osFamily,
            BuildType = buildType
        };

        Assert.ThrowsAsync<MissingInputException>(
            () =>
                BuildCreateHandler.BuildCreateAsync(
                    input,
                    MockUnityEnvironment.Object,
                    GameServerHostingService!,
                    MockLogger!.Object,
                    CancellationToken.None
                )
        );

        BuildsApi!.DefaultBuildsClient.Verify(
            api => api.CreateBuildAsync(
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<CreateBuildRequest>(),
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
        ValidProjectId,
        ValidEnvironmentName,
        ValidBuildName,
        ValidBuildVersionName,
        OsFamilyEnum.LINUX,
        BuildTypeEnum.FILEUPLOAD)]
    [TestCase(
        ValidProjectId,
        ValidEnvironmentName,
        ValidBuildName,
        ValidBuildVersionName,
        OsFamilyEnum.LINUX,
        BuildTypeEnum.CONTAINER)]
    [TestCase(
        ValidProjectId,
        ValidEnvironmentName,
        ValidBuildName,
        ValidBuildVersionName,
        OsFamilyEnum.LINUX,
        BuildTypeEnum.S3)]
    public async Task BuildCreateAsync_CallsGetService(
        string projectId,
        string environmentName,
        string buildName,
        string buildVersionName,
        OsFamilyEnum osFamily,
        BuildTypeEnum buildType
    )
    {
        BuildCreateInput input = new()
        {
            CloudProjectId = projectId,
            TargetEnvironmentName = environmentName,
            BuildName = buildName,
            BuildOsFamily = osFamily,
            BuildType = buildType,
            BuildVersionName = buildVersionName
        };

        await BuildCreateHandler.BuildCreateAsync(
            input,
            MockUnityEnvironment.Object,
            GameServerHostingService!,
            MockLogger!.Object,
            CancellationToken.None
        );

        BuildsApi!.DefaultBuildsClient.Verify(
            api => api.CreateBuildAsync(
                new Guid(input.CloudProjectId),
                new Guid(ValidEnvironmentId),
                new CreateBuildRequest(
                    buildName,
                    buildType,
                    buildVersionName,
                    null!,
                    osFamily),
                0,
                CancellationToken.None
            ),
            Times.Once);
    }

    [TestCase(
        InvalidProjectId,
        InvalidEnvironmentId,
        ValidBuildName,
        OsFamilyEnum.LINUX,
        BuildTypeEnum.FILEUPLOAD)]
    [TestCase(
        ValidProjectId,
        InvalidEnvironmentId,
        ValidBuildName,
        OsFamilyEnum.LINUX,
        BuildTypeEnum.FILEUPLOAD)]
    [TestCase(
        InvalidProjectId,
        ValidEnvironmentId,
        ValidBuildName,
        OsFamilyEnum.LINUX,
        BuildTypeEnum.FILEUPLOAD)]
    [TestCase(
        InvalidProjectId,
        InvalidEnvironmentId,
        ValidBuildName,
        OsFamilyEnum.LINUX,
        BuildTypeEnum.FILEUPLOAD)]
    [TestCase(
        InvalidProjectId,
        InvalidEnvironmentId,
        ValidBuildName,
        OsFamilyEnum.LINUX,
        BuildTypeEnum.CONTAINER)]
    [TestCase(
        ValidProjectId,
        InvalidEnvironmentId,
        ValidBuildName,
        OsFamilyEnum.LINUX,
        BuildTypeEnum.CONTAINER)]
    [TestCase(
        InvalidProjectId,
        ValidEnvironmentId,
        ValidBuildName,
        OsFamilyEnum.LINUX,
        BuildTypeEnum.CONTAINER)]
    [TestCase(
        InvalidProjectId,
        InvalidEnvironmentId,
        ValidBuildName,
        OsFamilyEnum.LINUX,
        BuildTypeEnum.CONTAINER)]
    [TestCase(
        InvalidProjectId,
        InvalidEnvironmentId,
        ValidBuildName,
        OsFamilyEnum.LINUX,
        BuildTypeEnum.S3)]
    [TestCase(
        ValidProjectId,
        InvalidEnvironmentId,
        ValidBuildName,
        OsFamilyEnum.LINUX,
        BuildTypeEnum.S3)]
    [TestCase(
        InvalidProjectId,
        ValidEnvironmentId,
        ValidBuildName,
        OsFamilyEnum.LINUX,
        BuildTypeEnum.S3)]
    [TestCase(
        InvalidProjectId,
        InvalidEnvironmentId,
        ValidBuildName,
        OsFamilyEnum.LINUX,
        BuildTypeEnum.S3)]
    public void BuildCreateAsync_InvalidInputThrowsException(
        string projectId,
        string environmentId,
        string buildName,
        OsFamilyEnum osFamily,
        BuildTypeEnum buildType
    )
    {
        BuildCreateInput input = new()
        {
            CloudProjectId = projectId,
            TargetEnvironmentName = ValidEnvironmentName,
            BuildName = buildName,
            BuildOsFamily = osFamily,
            BuildType = buildType
        };
        MockUnityEnvironment.Setup(ex => ex.FetchIdentifierAsync(CancellationToken.None)).ReturnsAsync(environmentId);

        Assert.ThrowsAsync<HttpRequestException>(
            () =>
                BuildCreateHandler.BuildCreateAsync(
                    input,
                    MockUnityEnvironment.Object,
                    GameServerHostingService!,
                    MockLogger!.Object,
                    CancellationToken.None
                )
        );

        TestsHelper.VerifyLoggerWasCalled(
            MockLogger!,
            LogLevel.Critical,
            LoggerExtension.ResultEventId,
            Times.Never);
    }

    [TestCase(
        ValidProjectId,
        ValidEnvironmentId,
        "Build1",
        OsFamilyEnum.LINUX,
        BuildTypeEnum.FILEUPLOAD)]
    [TestCase(
        ValidProjectId,
        ValidEnvironmentId,
        "Build1",
        OsFamilyEnum.LINUX,
        BuildTypeEnum.CONTAINER)]
    public void BuildCreateAsync_DuplicateNameThrowsException(
        string projectId,
        string environmentId,
        string buildName,
        OsFamilyEnum osFamily,
        BuildTypeEnum buildType
    )
    {
        BuildCreateInput input = new()
        {
            CloudProjectId = projectId,
            TargetEnvironmentName = ValidEnvironmentName,
            BuildName = buildName,
            BuildOsFamily = osFamily,
            BuildType = buildType
        };
        MockUnityEnvironment.Setup(ex => ex.FetchIdentifierAsync(CancellationToken.None)).ReturnsAsync(environmentId);

        Assert.ThrowsAsync<ApiException>(
            () =>
                BuildCreateHandler.BuildCreateAsync(
                    input,
                    MockUnityEnvironment.Object,
                    GameServerHostingService!,
                    MockLogger!.Object,
                    CancellationToken.None
                )
        );

        TestsHelper.VerifyLoggerWasCalled(
            MockLogger!,
            LogLevel.Critical,
            LoggerExtension.ResultEventId,
            Times.Never);
    }

    [TestCase(BuildTypeEnum.FILEUPLOAD)]
    [TestCase(BuildTypeEnum.CONTAINER)]
    [TestCase(BuildTypeEnum.S3)]
    public void BuildCreateAsync_InvalidBuildVersionName(
        BuildTypeEnum buildType
    )
    {
        BuildCreateInput input = new()
        {
            CloudProjectId = ValidProjectId,
            TargetEnvironmentName = ValidEnvironmentId,
            BuildName = ValidBuildName,
            BuildOsFamily = OsFamilyEnum.LINUX,
            BuildType = buildType,
            BuildVersionName = InValidBuildVersionName
        };
        MockUnityEnvironment.Setup(ex => ex.FetchIdentifierAsync(CancellationToken.None))
            .ReturnsAsync(ValidEnvironmentId);

        Assert.ThrowsAsync<CliException>(
            () =>
                BuildCreateHandler.BuildCreateAsync(
                    input,
                    MockUnityEnvironment.Object,
                    GameServerHostingService!,
                    MockLogger!.Object,
                    CancellationToken.None
                )
        );

        TestsHelper.VerifyLoggerWasCalled(
            MockLogger!,
            LogLevel.Critical,
            LoggerExtension.ResultEventId,
            Times.Never);
    }
}
