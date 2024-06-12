using Moq;
using NUnit.Framework;
using Unity.Services.Cli.CloudSave.Service;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using Unity.Services.Cli.Common.Console;
using Unity.Services.Cli.Common.Logging;
using Unity.Services.Cli.Common.Utils;
using Unity.Services.Cli.CloudSave.Handlers;
using Unity.Services.Cli.CloudSave.Input;
using Unity.Services.Cli.CloudSave.UnitTest.Utils;
using Unity.Services.Gateway.CloudSaveApiV1.Generated.Model;
using Unity.Services.Cli.TestUtils;

namespace Unity.Services.Cli.CloudSave.UnitTest.Handlers;

public class ListPlayerIdsHandlerTests
{
    readonly Mock<ICloudSaveDataService> m_MockCloudSaveDataService = new();
    readonly Mock<IUnityEnvironment> m_MockUnityEnvironment = new();
    readonly Mock<ILogger> m_MockLogger = new();

    [SetUp]
    public void SetUp()
    {
        m_MockUnityEnvironment.Reset();
        m_MockLogger.Reset();
        m_MockCloudSaveDataService.Reset();
    }

    [Test]
    public async Task ListPlayerDataIdsHandler_CallsLoadingIndicator()
    {
        Mock<ILoadingIndicator> mockLoadingIndicator = new Mock<ILoadingIndicator>();

        await ListPlayerDataIdsHandler.ListPlayerDataIdsAsync(null!, null!, null!, null!, mockLoadingIndicator.Object, CancellationToken.None);

        mockLoadingIndicator.Verify(ex => ex
            .StartLoadingAsync(It.IsAny<string>(), It.IsAny<Func<StatusContext?, Task>>()), Times.Once);
    }

    [Test]
    public async Task ListPlayerDataIdsHandler_CallsServiceAndLogger_WhenInputIsValid()
    {
        ListDataIdsInput input = new()
        {
            CloudProjectId = TestValues.ValidProjectId,
            Start = "someStart",
            Limit = 2
        };

        m_MockUnityEnvironment.Setup(x => x.FetchIdentifierAsync(CancellationToken.None))
            .ReturnsAsync(TestValues.ValidEnvironmentId);

        var result = new GetPlayersWithDataResponse(
            new List<GetPlayersWithDataResponseResultsInner>
            {
                new GetPlayersWithDataResponseResultsInner(
                    "testId1",
                    new AccessClassesWithMetadata
                    {
                        Private = new AccessClassMetadata
                        {
                            NumKeys = 1,
                            TotalSize = 100
                        },
                        Protected = new AccessClassMetadata
                        {
                            NumKeys = 2,
                            TotalSize = 200
                        }
                    }
                ),
                new GetPlayersWithDataResponseResultsInner(
                    "testId2",
                    new AccessClassesWithMetadata
                    {
                        Default = new AccessClassMetadata
                        {
                            NumKeys = 3,
                            TotalSize = 300
                        }
                    }
                ),
            },
            new GetPlayersWithDataResponseLinks("someLink")
        );

        m_MockCloudSaveDataService.Setup(x => x.ListPlayerDataIdsAsync(TestValues.ValidProjectId, TestValues.ValidEnvironmentId, input.Start, input.Limit, CancellationToken.None))
            .ReturnsAsync(result);

        await ListPlayerDataIdsHandler.ListPlayerDataIdsAsync(
            input,
            m_MockUnityEnvironment.Object,
            m_MockCloudSaveDataService!.Object,
            m_MockLogger!.Object,
            CancellationToken.None
        );

        m_MockUnityEnvironment.Verify(x => x.FetchIdentifierAsync(CancellationToken.None), Times.Once);
        m_MockCloudSaveDataService.Verify(ex => ex.ListPlayerDataIdsAsync(TestValues.ValidProjectId, TestValues.ValidEnvironmentId,
            input.Start, input.Limit, CancellationToken.None), Times.Once);

        TestsHelper.VerifyLoggerWasCalled(m_MockLogger, LogLevel.Critical, LoggerExtension.ResultEventId, Times.Once);
    }
}
