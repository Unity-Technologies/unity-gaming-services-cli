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
using Unity.Services.Cli.CloudSave.UnitTest.Utils;
using Unity.Services.Cli.CloudSave.Utils;
using Unity.Services.Cli.Common.Exceptions;
using Unity.Services.Gateway.CloudSaveApiV1.Generated.Model;
using Unity.Services.Cli.TestUtils;

namespace Unity.Services.Cli.CloudSave.UnitTest.Handlers;

public class GetPlayerDataItemsHandlerTests
{
    readonly Mock<ICloudSaveDataService> m_MockCloudSaveDataService = new();
    readonly Mock<IUnityEnvironment> m_MockUnityEnvironment = new();
    readonly Mock<ILogger> m_MockLogger = new();

    static readonly List<string> k_ValidKeyList = new List<string>()
    {
        "key1",
        "key2"
    };
    static readonly string k_ValidAfterValue = "after";
    static readonly GetItemsResponse k_ValidResponse = new GetItemsResponse(
        new List<Item>()
        {
            new Item("key1", "value1", "writelock1", new ModifiedMetadata(DateTime.Now), new ModifiedMetadata(DateTime.Today)),
            new Item("key2", "value2", "writelock2", new ModifiedMetadata(DateTime.Now), new ModifiedMetadata(DateTime.Today))
        },
        new GetItemsResponseLinks("nextValue")
    );

    [SetUp]
    public void SetUp()
    {
        m_MockUnityEnvironment.Reset();
        m_MockLogger.Reset();
        m_MockCloudSaveDataService.Reset();
    }

    [Test]
    public async Task GetPlayerDataItemHandler_CallsLoadingIndicator()
    {
        Mock<ILoadingIndicator> mockLoadingIndicator = new Mock<ILoadingIndicator>();

        await GetPlayerDataItemsHandler.GetPlayerDataItemsAsync(null!, null!, null!, null!, mockLoadingIndicator.Object, CancellationToken.None);

        mockLoadingIndicator.Verify(ex => ex
            .StartLoadingAsync(It.IsAny<string>(), It.IsAny<Func<StatusContext?, Task>>()), Times.Once);
    }

    [Test]
    public async Task GlayerDataItemsHandler_CallsServiceAndLogger_WhenInputIsValid_String()
    {
        GetPlayerItemsInput input = new()
        {
            CloudProjectId = TestValues.ValidProjectId,
            PlayerId = TestValues.ValidPlayerId,
            Keys = k_ValidKeyList,
            After = k_ValidAfterValue,
            Visibility = "public"
        };

        m_MockUnityEnvironment.Setup(x => x.FetchIdentifierAsync(CancellationToken.None))
            .ReturnsAsync(TestValues.ValidEnvironmentId);

        m_MockCloudSaveDataService.Setup(x => x.GetPlayerDataItemsAsync(TestValues.ValidProjectId,
            TestValues.ValidEnvironmentId,
            TestValues.ValidPlayerId,
            k_ValidKeyList,
            k_ValidAfterValue,
            PlayerIndexVisibilityTypes.Public,
            CancellationToken.None))
            .ReturnsAsync(k_ValidResponse);

        await GetPlayerDataItemsHandler.GetPlayerDataItemsAsync(
            input,
            m_MockUnityEnvironment.Object,
            m_MockCloudSaveDataService!.Object,
            m_MockLogger!.Object,
            CancellationToken.None
        );

        m_MockUnityEnvironment.Verify(x => x.FetchIdentifierAsync(CancellationToken.None), Times.Once);
        m_MockCloudSaveDataService.Verify(ex => ex.GetPlayerDataItemsAsync(TestValues.ValidProjectId, TestValues.ValidEnvironmentId,
            TestValues.ValidPlayerId, k_ValidKeyList, k_ValidAfterValue, PlayerIndexVisibilityTypes.Public,
            CancellationToken.None), Times.Once);

        TestsHelper.VerifyLoggerWasCalled(m_MockLogger, LogLevel.Critical, LoggerExtension.ResultEventId, Times.Once);
    }

    [Test]
    public async Task GetPlayerDataItemsHandler_CallsServiceAndLogger_WhenInputIsValid_EmptyKeyList()
    {
        GetPlayerItemsInput input = new()
        {
            CloudProjectId = TestValues.ValidProjectId,
            PlayerId = TestValues.ValidPlayerId,
            Keys = null,
            After = k_ValidAfterValue,
            Visibility = "public"
        };

        m_MockUnityEnvironment.Setup(x => x.FetchIdentifierAsync(CancellationToken.None))
            .ReturnsAsync(TestValues.ValidEnvironmentId);

        m_MockCloudSaveDataService.Setup(x => x.GetPlayerDataItemsAsync(TestValues.ValidProjectId,
                TestValues.ValidEnvironmentId,
                TestValues.ValidPlayerId,
                null,
                k_ValidAfterValue,
                PlayerIndexVisibilityTypes.Public,
                CancellationToken.None))
            .ReturnsAsync(k_ValidResponse);

        await GetPlayerDataItemsHandler.GetPlayerDataItemsAsync(
            input,
            m_MockUnityEnvironment.Object,
            m_MockCloudSaveDataService!.Object,
            m_MockLogger!.Object,
            CancellationToken.None
        );

        m_MockUnityEnvironment.Verify(x => x.FetchIdentifierAsync(CancellationToken.None), Times.Once);
        m_MockCloudSaveDataService.Verify(ex => ex.GetPlayerDataItemsAsync(TestValues.ValidProjectId, TestValues.ValidEnvironmentId,
            TestValues.ValidPlayerId, null, k_ValidAfterValue, PlayerIndexVisibilityTypes.Public,
            CancellationToken.None), Times.Once);

        TestsHelper.VerifyLoggerWasCalled(m_MockLogger, LogLevel.Critical, LoggerExtension.ResultEventId, Times.Once);
    }

    [Test]
    public async Task GetPlayerDataItemsHandler_CallsServiceAndLogger_WhenInputIsValid_EmptyResponse()
    {
        GetPlayerItemsInput input = new()
        {
            CloudProjectId = TestValues.ValidProjectId,
            PlayerId = TestValues.ValidPlayerId,
            Keys = null,
            After = k_ValidAfterValue,
            Visibility = "public"
        };

        var emptyResponse = new GetItemsResponse(new List<Item>(), new GetItemsResponseLinks(""));

        m_MockUnityEnvironment.Setup(x => x.FetchIdentifierAsync(CancellationToken.None))
            .ReturnsAsync(TestValues.ValidEnvironmentId);

        m_MockCloudSaveDataService.Setup(x => x.GetPlayerDataItemsAsync(TestValues.ValidProjectId,
                TestValues.ValidEnvironmentId,
                TestValues.ValidPlayerId,
                null,
                k_ValidAfterValue,
                PlayerIndexVisibilityTypes.Public,
                CancellationToken.None))
            .ReturnsAsync(emptyResponse);

        await GetPlayerDataItemsHandler.GetPlayerDataItemsAsync(
            input,
            m_MockUnityEnvironment.Object,
            m_MockCloudSaveDataService!.Object,
            m_MockLogger!.Object,
            CancellationToken.None
        );

        m_MockUnityEnvironment.Verify(x => x.FetchIdentifierAsync(CancellationToken.None), Times.Once);
        m_MockCloudSaveDataService.Verify(ex => ex.GetPlayerDataItemsAsync(TestValues.ValidProjectId, TestValues.ValidEnvironmentId,
            TestValues.ValidPlayerId, null, k_ValidAfterValue, PlayerIndexVisibilityTypes.Public,
            CancellationToken.None), Times.Once);

        TestsHelper.VerifyLoggerWasCalled(m_MockLogger, LogLevel.Critical, LoggerExtension.ResultEventId, Times.Once);
    }

    [Test]
    public async Task GetPlayerDataItemsHandler_CallsServiceAndLogger_UsesDefaultVisibilityWhenNotSet()
    {
        GetPlayerItemsInput input = new()
        {
            CloudProjectId = TestValues.ValidProjectId,
            PlayerId = TestValues.ValidPlayerId,
            Keys = k_ValidKeyList,
            After = k_ValidAfterValue,
        };

        m_MockUnityEnvironment.Setup(x => x.FetchIdentifierAsync(CancellationToken.None))
            .ReturnsAsync(TestValues.ValidEnvironmentId);

        m_MockCloudSaveDataService.Setup(x => x.GetPlayerDataItemsAsync(TestValues.ValidProjectId,
                TestValues.ValidEnvironmentId,
                TestValues.ValidPlayerId,
                k_ValidKeyList,
                k_ValidAfterValue,
                PlayerIndexVisibilityTypes.Default,
                CancellationToken.None))
            .ReturnsAsync(k_ValidResponse);

        await GetPlayerDataItemsHandler.GetPlayerDataItemsAsync(
            input,
            m_MockUnityEnvironment.Object,
            m_MockCloudSaveDataService!.Object,
            m_MockLogger!.Object,
            CancellationToken.None
        );

        m_MockUnityEnvironment.Verify(x => x.FetchIdentifierAsync(CancellationToken.None), Times.Once);
        m_MockCloudSaveDataService.Verify(ex => ex.GetPlayerDataItemsAsync(TestValues.ValidProjectId, TestValues.ValidEnvironmentId,
            TestValues.ValidPlayerId, k_ValidKeyList, k_ValidAfterValue, PlayerIndexVisibilityTypes.Default,
            CancellationToken.None), Times.Once);

        TestsHelper.VerifyLoggerWasCalled(m_MockLogger, LogLevel.Critical, LoggerExtension.ResultEventId, Times.Once);
    }

    [Test]
    public void GetPlayerDataItemsHandler_InvalidVisibilityThrowsException()
    {
        GetPlayerItemsInput input = new()
        {
            CloudProjectId = TestValues.ValidProjectId,
            PlayerId = TestValues.ValidPlayerId,
            Keys = null,
            After = k_ValidAfterValue,
            Visibility = "some-invalid-visibility"
        };
        Assert.ThrowsAsync<CliException>(async () => await GetPlayerDataItemsHandler.GetPlayerDataItemsAsync(input, m_MockUnityEnvironment.Object, m_MockCloudSaveDataService.Object, m_MockLogger.Object, default));
    }
}
