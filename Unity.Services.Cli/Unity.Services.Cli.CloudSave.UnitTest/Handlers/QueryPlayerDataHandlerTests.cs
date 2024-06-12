using Moq;
using NUnit.Framework;
using Unity.Services.Cli.CloudSave.Service;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Spectre.Console;
using Unity.Services.Cli.Common.Console;
using Unity.Services.Cli.Common.Logging;
using Unity.Services.Cli.Common.Utils;
using Unity.Services.Cli.CloudSave.Handlers;
using Unity.Services.Cli.CloudSave.Input;
using Unity.Services.Cli.Common.Exceptions;
using Unity.Services.Gateway.CloudSaveApiV1.Generated.Model;
using Unity.Services.Cli.TestUtils;

namespace Unity.Services.Cli.CloudSave.UnitTest.Handlers;

public class QueryPlayerDataHandlerTests
{
    readonly Mock<ICloudSaveDataService> m_MockCloudSaveDataService = new();
    readonly Mock<IUnityEnvironment> m_MockUnityEnvironment = new();
    readonly Mock<ILogger> m_MockLogger = new();

    readonly QueryIndexBody m_ValidQueryIndexBody = new QueryIndexBody(
        new List<FieldFilter>()
            { new FieldFilter ("fieldFilter_key","fieldFilter_value", FieldFilter.OpEnum.EQ, true)},
        new List<string> { "returnKey1", "returnKey2" },
        5,
        10);

    readonly List<QueryIndexResponseResultsInner> m_ValidQueryResponse = new List<QueryIndexResponseResultsInner>()
    {
        new QueryIndexResponseResultsInner("id",
            new List<Item>() {
                new Item("key1", "value", "writelock", new ModifiedMetadata(DateTime.Now), new ModifiedMetadata(DateTime.Today))
            }
        )
    };

    [SetUp]
    public void SetUp()
    {
        m_MockUnityEnvironment.Reset();
        m_MockLogger.Reset();
        m_MockCloudSaveDataService.Reset();
        m_MockCloudSaveDataService.Setup(l =>
                l.QueryPlayerDataAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    CancellationToken.None))
            .Returns(Task.FromResult(new QueryIndexResponse(m_ValidQueryResponse)));
    }

    [Test]
    public async Task QueryPlayerData_CallsLoadingIndicator()
    {
        Mock<ILoadingIndicator> mockLoadingIndicator = new Mock<ILoadingIndicator>();

        await QueryPlayerDataHandler.QueryPlayerDataAsync(null!, null!, null!, null!, mockLoadingIndicator.Object, CancellationToken.None);

        mockLoadingIndicator.Verify(ex => ex
            .StartLoadingAsync(It.IsAny<string>(), It.IsAny<Func<StatusContext?, Task>>()), Times.Once);
    }

    [Test]
    public void QueryPlayerDataHandler_HandlesInputAndLogsOnSuccess()
    {
        var inputBody = JsonConvert.SerializeObject(m_ValidQueryIndexBody);
        var input = new QueryDataInput()
        {
            JsonFileOrBody = inputBody,
        };
        Assert.DoesNotThrowAsync(async () => await QueryPlayerDataHandler.QueryPlayerDataAsync(input, m_MockUnityEnvironment.Object, m_MockCloudSaveDataService.Object, m_MockLogger.Object, default));
        TestsHelper.VerifyLoggerWasCalled(m_MockLogger, LogLevel.Critical, LoggerExtension.ResultEventId, Times.Once);
    }

    [Test]
    public void QueryPlayerDataHandler_MissingBodyThrowsException()
    {
        var input = new QueryDataInput();
        Assert.ThrowsAsync<CliException>(async () => await QueryPlayerDataHandler.QueryPlayerDataAsync(input, m_MockUnityEnvironment.Object, m_MockCloudSaveDataService.Object, m_MockLogger.Object, default));
    }
}
