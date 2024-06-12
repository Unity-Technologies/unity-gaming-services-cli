using Moq;
using NUnit.Framework;
using Unity.Services.Cli.CloudSave.Service;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using Unity.Services.Cli.Common.Console;
using Unity.Services.Cli.Common.Input;
using Unity.Services.Cli.Common.Logging;
using Unity.Services.Cli.Common.Utils;
using Unity.Services.Cli.CloudSave.Handlers;
using Unity.Services.Cli.CloudSave.UnitTest.Utils;
using Unity.Services.Gateway.CloudSaveApiV1.Generated.Model;
using Unity.Services.Cli.TestUtils;

namespace Unity.Services.Cli.CloudSave.UnitTest.Handlers;

public class ListIndexesHandlerTests
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
    public async Task ListIndexes_CallsLoadingIndicator()
    {
        Mock<ILoadingIndicator> mockLoadingIndicator = new Mock<ILoadingIndicator>();

        await ListIndexesHandler.ListIndexesAsync(null!, null!, null!, null!, mockLoadingIndicator.Object, CancellationToken.None);

        mockLoadingIndicator.Verify(ex => ex
            .StartLoadingAsync(It.IsAny<string>(), It.IsAny<Func<StatusContext?, Task>>()), Times.Once);
    }

    [Test]
    public async Task ListIndexesHandler_CallsServiceAndLogger_WhenInputIsValid()
    {
        CommonInput input = new()
        {
            CloudProjectId = TestValues.ValidProjectId,
        };


        m_MockUnityEnvironment.Setup(x => x.FetchIdentifierAsync(CancellationToken.None))
            .ReturnsAsync(TestValues.ValidEnvironmentId);

        var result = new List<LiveIndexConfigInner>
        {
            new LiveIndexConfigInner(
                "testIndex1",
                LiveIndexConfigInner.EntityTypeEnum.Player,
                AccessClass.Default,
                IndexStatus.READY,
                new List<IndexField>()
                {
                    new IndexField("testIndexKey1", true)
                }
            ),
            new LiveIndexConfigInner(
                "testIndex2",
                LiveIndexConfigInner.EntityTypeEnum.Custom,
                AccessClass.Private,
                IndexStatus.BUILDING,
                new List<IndexField>()
                {
                    new IndexField("testIndexKey2", false)
                }
            ),
        };

        GetIndexIdsResponse response = new GetIndexIdsResponse(result);
        m_MockCloudSaveDataService.Setup(x => x.ListIndexesAsync(TestValues.ValidProjectId, TestValues.ValidEnvironmentId, CancellationToken.None))
            .ReturnsAsync(response);

        await ListIndexesHandler.ListIndexesAsync(
            input,
            m_MockUnityEnvironment.Object,
            m_MockCloudSaveDataService!.Object,
            m_MockLogger!.Object,
            CancellationToken.None
        );

        m_MockUnityEnvironment.Verify(x => x.FetchIdentifierAsync(CancellationToken.None), Times.Once);
        m_MockCloudSaveDataService.Verify(ex => ex.ListIndexesAsync(TestValues.ValidProjectId, TestValues.ValidEnvironmentId,
            CancellationToken.None), Times.Once);

        TestsHelper.VerifyLoggerWasCalled(m_MockLogger, LogLevel.Critical, LoggerExtension.ResultEventId, Times.Once);
    }

}
