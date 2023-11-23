using Microsoft.Extensions.Logging;
using Moq;
using Spectre.Console;
using Unity.Services.Cli.Common.Console;
using Unity.Services.Cli.Common.Logging;
using Unity.Services.Cli.GameServerHosting.Exceptions;
using Unity.Services.Cli.GameServerHosting.Handlers;
using Unity.Services.Cli.GameServerHosting.Input;
using Unity.Services.Cli.TestUtils;
using Unity.Services.Gateway.GameServerHostingApiV1.Generated.Model;

namespace Unity.Services.Cli.GameServerHosting.UnitTest.Handlers;

[TestFixture]
class FileDownloadHandlerTests : HandlerCommon
{
    [Test]
    public async Task FileDownloadAsync_CallsLoadingIndicatorStartLoading()
    {
        var mockLoadingIndicator = new Mock<ILoadingIndicator>();

        await FileDownloadHandler.FileDownloadAsync(
            null!,
            MockUnityEnvironment.Object,
            null!,
            null!,
            null!,
            mockLoadingIndicator.Object,
            CancellationToken.None);

        mockLoadingIndicator.Verify(
            ex => ex.StartLoadingAsync(
                It.IsAny<string>(),
                It.IsAny<Func<StatusContext?, Task>>()
            ),
            Times.Once
        );
    }

    [Test]
    public async Task FileDownloadAsync_CallsFetchIdentifierAsync()
    {
        FileDownloadInput input = new()
        {
            CloudProjectId = ValidProjectId,
            TargetEnvironmentName = ValidEnvironmentName,
            ServerId = ValidServerId.ToString(),
            Path = "some/path",
            Output = ValidOutputDirectory,
        };

        await FileDownloadHandler.FileDownloadAsync(
            input,
            MockUnityEnvironment.Object,
            GameServerHostingService!,
            MockLogger!.Object,
            MockHttpClient!.Object,
            CancellationToken.None
        );

        MockUnityEnvironment.Verify(ex => ex.FetchIdentifierAsync(CancellationToken.None), Times.Once);
    }

    [Test]
    public async Task FileDownloadAsync_CallsAuthorizeGameServerHostingService()
    {
        FileDownloadInput input = new()
        {
            CloudProjectId = ValidProjectId,
            TargetEnvironmentName = ValidEnvironmentName,
            ServerId = ValidServerId.ToString(),
            Path = "some/path",
            Output = ValidOutputDirectory,
        };

        await FileDownloadHandler.FileDownloadAsync(
            input,
            MockUnityEnvironment.Object,
            GameServerHostingService!,
            MockLogger!.Object,
            MockHttpClient!.Object,
            CancellationToken.None
        );

        MockUnityEnvironment.Verify(ex => ex.FetchIdentifierAsync(CancellationToken.None), Times.Once);
    }

    [Test]
    public async Task FileDownloadAsync_CallsGenerateDownloadURLAsync()
    {
        FileDownloadInput input = new()
        {
            CloudProjectId = ValidProjectId,
            TargetEnvironmentName = ValidEnvironmentName,
            ServerId = ValidServerId.ToString(),
            Path = "some/path",
            Output = ValidOutputDirectory,
        };

        await FileDownloadHandler.FileDownloadAsync(
            input,
            MockUnityEnvironment.Object,
            GameServerHostingService!,
            MockLogger!.Object,
            MockHttpClient!.Object,
            CancellationToken.None
        );

        FilesApi!.DefaultFilesClient.Verify(
            api => api.GenerateDownloadURLAsync(
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<GenerateDownloadURLRequest>(),
                0,
                It.IsAny<CancellationToken>()
            ),
            Times.Once
        );
    }

    [TestCase(
        null,
        "some/path",
        ValidOutputDirectory,
        typeof(ArgumentNullException),
        TestName = "ServerId is null")]
    [TestCase(
        "1",
        null,
        ValidOutputDirectory,
        typeof(ArgumentNullException),
        TestName = "Path is null")]
    [TestCase(
        "1",
        "some/path",
        null,
        typeof(MissingInputException),
        TestName = "Output is null")]
    public void FileDownloadAsync_InvalidInputThrowsException(
        string? serverId,
        string? path,
        string? output,
        Type exceptionType
    )
    {
        FileDownloadInput input = new()
        {
            CloudProjectId = ValidProjectId,
            TargetEnvironmentName = ValidEnvironmentName,
            ServerId = serverId,
            Path = path,
            Output = output,
        };

        Assert.ThrowsAsync(
            exceptionType,
            async () =>
            {
                await FileDownloadHandler.FileDownloadAsync(
                    input,
                    MockUnityEnvironment.Object,
                    GameServerHostingService!,
                    MockLogger!.Object,
                    MockHttpClient!.Object,
                    CancellationToken.None
                );
            }
        );

        TestsHelper.VerifyLoggerWasCalled(
            MockLogger!,
            LogLevel.Critical,
            LoggerExtension.ResultEventId,
            Times.Never
        );
    }
}
