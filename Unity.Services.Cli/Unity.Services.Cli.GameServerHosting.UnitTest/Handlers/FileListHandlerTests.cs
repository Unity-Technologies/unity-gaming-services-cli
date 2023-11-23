using System.Globalization;
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
class FileListHandlerTests : HandlerCommon
{
    [Test]
    public async Task FilesListAsync_CallsLoadingIndicatorStartLoading()
    {
        var mockLoadingIndicator = new Mock<ILoadingIndicator>();

        await FileListHandler.FileListAsync(
            null!,
            MockUnityEnvironment.Object,
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
    public async Task FilesListAsync_CallsFetchIdentifierAsync()
    {
        FileListInput input = new()
        {
            CloudProjectId = ValidProjectId,
            TargetEnvironmentName = ValidEnvironmentName,
            ServerIds = new[] { ValidServerId },
            Limit = "100",
            ModifiedFrom = "2017-07-21T17:32:25Z",
            ModifiedTo = "2017-07-22T17:32:25Z",
            PathFilter = "",
        };

        await FileListHandler.FileListAsync(
            input,
            MockUnityEnvironment.Object,
            GameServerHostingService!,
            MockLogger!.Object,
            CancellationToken.None
        );

        MockUnityEnvironment.Verify(ex => ex.FetchIdentifierAsync(CancellationToken.None), Times.Once);
    }

    [TestCase(1, "2017-07-21T17:32:25Z", "2017-07-21T17:32:25Z", "/games/tf2/", new[] { ValidServerId }, TestName = "Golden path")]
    public async Task FilesListAsync_CallsListService(
        long limit,
        string modifiedFrom,
        string modifiedTo,
        string pathFilter,
        long[] serverIds
    )
    {
        FileListInput input = new()
        {
            CloudProjectId = ValidProjectId,
            TargetEnvironmentName = ValidEnvironmentName,
            Limit = limit.ToString(),
            ModifiedFrom = modifiedFrom,
            ModifiedTo = modifiedTo,
            PathFilter = pathFilter,
            ServerIds = serverIds
        };

        await FileListHandler.FileListAsync(
            input,
            MockUnityEnvironment.Object,
            GameServerHostingService!,
            MockLogger!.Object,
            CancellationToken.None
        );

        FilesApi!.DefaultFilesClient.Verify(
            api => api.ListFilesAsync(
                new Guid(ValidProjectId),
                new Guid(ValidEnvironmentId),
                new FilesListRequest(
                    limit,
                    DateTime.Parse(modifiedFrom).ToUniversalTime(),
                    DateTime.Parse(modifiedFrom).ToUniversalTime(),
                    pathFilter,
                    serverIds.ToList()
                ),
                0,
                CancellationToken.None
            ),
            Times.Once
        );
    }

    [TestCase(null, 1, "2017-07-21T17:32:25Z", "2017-07-21T17:32:25Z", "/games/tf2/", new[] { ValidServerId }, typeof(ArgumentNullException), TestName = "Null Project Id throws ArgumentNullException")]
    [TestCase(InvalidProjectId, 1, "2017-07-21T17:32:25Z", "2017-07-21T17:32:25Z", "/games/tf2/", new[] { ValidServerId }, typeof(HttpRequestException), TestName = "Invalid Project Id throws HttpRequestException")]
    [TestCase(ValidProjectId, 1, "2017-07-21T17:32:25Z", "2017-07-21T17:32:25Z", "/games/tf2/", null, typeof(MissingInputException), TestName = "Null Server Ids throws ArgumentNullException")]
    public void FilesListAsync_InvalidInputThrowsException(
        string? projectId,
        long limit,
        DateTime? modifiedFrom,
        DateTime? modifiedTo,
        string? pathFilter,
        long[]? serverIds,
        Type exceptionType
    )
    {
        FileListInput input = new()
        {
            CloudProjectId = projectId,
            TargetEnvironmentName = ValidEnvironmentName,
            Limit = limit.ToString(),
            ModifiedFrom = modifiedFrom == null ? null : modifiedFrom.ToString(),
            ModifiedTo = modifiedTo == null ? null : modifiedTo.ToString(),
            PathFilter = pathFilter,
            ServerIds = serverIds
        };

        Assert.ThrowsAsync(exceptionType, () =>
            FileListHandler.FileListAsync(
                input,
                MockUnityEnvironment.Object,
                GameServerHostingService!,
                MockLogger!.Object,
                CancellationToken.None
            )
        );

        TestsHelper.VerifyLoggerWasCalled(MockLogger!, LogLevel.Critical, LoggerExtension.ResultEventId, Times.Never);
    }
}


