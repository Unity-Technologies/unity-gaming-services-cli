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

public class SetPlayerDataItemHandlerTests
{
    readonly Mock<ICloudSaveDataService> m_MockCloudSaveDataService = new();
    readonly Mock<IUnityEnvironment> m_MockUnityEnvironment = new();
    readonly Mock<ILogger> m_MockLogger = new();

    static readonly string k_ValidKey = "key";
    static readonly string k_ValidStringValue = "value";
    static readonly string k_ValidNumberValue = "12";

    static readonly string k_ValidWriteLock = "writelock";

    [SetUp]
    public void SetUp()
    {
        m_MockUnityEnvironment.Reset();
        m_MockLogger.Reset();
        m_MockCloudSaveDataService.Reset();
    }

    [Test]
    public async Task SetPlayerDataItemHandler_CallsLoadingIndicator()
    {
        Mock<ILoadingIndicator> mockLoadingIndicator = new Mock<ILoadingIndicator>();

        await SetPlayerDataItemHandler.SetPlayerDataItemAsync(null!, null!, null!, null!, mockLoadingIndicator.Object, CancellationToken.None);

        mockLoadingIndicator.Verify(ex => ex
            .StartLoadingAsync(It.IsAny<string>(), It.IsAny<Func<StatusContext?, Task>>()), Times.Once);
    }

    [Test]
    public async Task SetPlayerDataItemHandler_CallsService_WhenInputIsValid_String()
    {
        SetPlayerItemInput input = new()
        {
            CloudProjectId = TestValues.ValidProjectId,
            PlayerId = TestValues.ValidPlayerId,
            Key = k_ValidKey,
            Value = k_ValidStringValue,
            WriteLock = k_ValidWriteLock,
            Visibility = "public"
        };

        m_MockUnityEnvironment.Setup(x => x.FetchIdentifierAsync(CancellationToken.None))
            .ReturnsAsync(TestValues.ValidEnvironmentId);

        var response = new SetItemResponse(k_ValidWriteLock);

        m_MockCloudSaveDataService.Setup(x => x.SetPlayerDataItemAsync(TestValues.ValidProjectId,
            TestValues.ValidEnvironmentId,
            TestValues.ValidPlayerId,
            k_ValidKey,
            k_ValidStringValue,
            k_ValidWriteLock,
            PlayerIndexVisibilityTypes.Public,
            CancellationToken.None))
            .ReturnsAsync(response);

        await SetPlayerDataItemHandler.SetPlayerDataItemAsync(
            input,
            m_MockUnityEnvironment.Object,
            m_MockCloudSaveDataService!.Object,
            m_MockLogger!.Object,
            CancellationToken.None
        );

        m_MockUnityEnvironment.Verify(x => x.FetchIdentifierAsync(CancellationToken.None), Times.Once);
        m_MockCloudSaveDataService.Verify(ex => ex.SetPlayerDataItemAsync(TestValues.ValidProjectId, TestValues.ValidEnvironmentId,
            TestValues.ValidPlayerId, k_ValidKey, k_ValidStringValue, k_ValidWriteLock, PlayerIndexVisibilityTypes.Public,
            CancellationToken.None), Times.Once);
    }

    [Test]
    public async Task SetPlayerDataItemHandler_CallsService_WhenInputIsValid_Number()
    {
        SetPlayerItemInput input = new()
        {
            CloudProjectId = TestValues.ValidProjectId,
            PlayerId = TestValues.ValidPlayerId,
            Key = k_ValidKey,
            Value = k_ValidNumberValue,
            WriteLock = k_ValidWriteLock,
            Visibility = "public"
        };

        m_MockUnityEnvironment.Setup(x => x.FetchIdentifierAsync(CancellationToken.None))
            .ReturnsAsync(TestValues.ValidEnvironmentId);

        var response = new SetItemResponse(k_ValidWriteLock);

        m_MockCloudSaveDataService.Setup(x => x.SetPlayerDataItemAsync(TestValues.ValidProjectId,
                TestValues.ValidEnvironmentId,
                TestValues.ValidPlayerId,
                k_ValidKey,
                long.Parse(k_ValidNumberValue),
                k_ValidWriteLock,
                PlayerIndexVisibilityTypes.Public,
                CancellationToken.None))
            .ReturnsAsync(response);

        await SetPlayerDataItemHandler.SetPlayerDataItemAsync(
            input,
            m_MockUnityEnvironment.Object,
            m_MockCloudSaveDataService!.Object,
            m_MockLogger!.Object,
            CancellationToken.None
        );

        m_MockUnityEnvironment.Verify(x => x.FetchIdentifierAsync(CancellationToken.None), Times.Once);
        m_MockCloudSaveDataService.Verify(ex => ex.SetPlayerDataItemAsync(TestValues.ValidProjectId, TestValues.ValidEnvironmentId,
            TestValues.ValidPlayerId, k_ValidKey, long.Parse(k_ValidNumberValue), k_ValidWriteLock, PlayerIndexVisibilityTypes.Public,
            CancellationToken.None), Times.Once);
    }

    [Test]
    public async Task SetPlayerDataItemHandler_CallsService_UsesDefaultVisibilityWhenNotSet()
    {
        SetPlayerItemInput input = new()
        {
            CloudProjectId = TestValues.ValidProjectId,
            PlayerId = TestValues.ValidPlayerId,
            Key = k_ValidKey,
            Value = k_ValidStringValue,
            WriteLock = k_ValidWriteLock,
        };

        m_MockUnityEnvironment.Setup(x => x.FetchIdentifierAsync(CancellationToken.None))
            .ReturnsAsync(TestValues.ValidEnvironmentId);

        var response = new SetItemResponse(k_ValidWriteLock);

        m_MockCloudSaveDataService.Setup(x => x.SetPlayerDataItemAsync(TestValues.ValidProjectId,
                TestValues.ValidEnvironmentId,
                TestValues.ValidPlayerId,
                k_ValidKey,
                k_ValidStringValue,
                k_ValidWriteLock,
                PlayerIndexVisibilityTypes.Default,
                CancellationToken.None))
            .ReturnsAsync(response);

        await SetPlayerDataItemHandler.SetPlayerDataItemAsync(
            input,
            m_MockUnityEnvironment.Object,
            m_MockCloudSaveDataService!.Object,
            m_MockLogger!.Object,
            CancellationToken.None
        );

        m_MockUnityEnvironment.Verify(x => x.FetchIdentifierAsync(CancellationToken.None), Times.Once);
        m_MockCloudSaveDataService.Verify(ex => ex.SetPlayerDataItemAsync(TestValues.ValidProjectId, TestValues.ValidEnvironmentId,
            TestValues.ValidPlayerId, k_ValidKey, k_ValidStringValue, k_ValidWriteLock, PlayerIndexVisibilityTypes.Default,
            CancellationToken.None), Times.Once);
    }

    [Test]
    public void SetPlayerDataItemHandler_InvalidVisibilityThrowsException()
    {
        SetPlayerItemInput input = new()
        {
            CloudProjectId = TestValues.ValidProjectId,
            PlayerId = TestValues.ValidPlayerId,
            Key = k_ValidKey,
            Value = k_ValidStringValue,
            Visibility = "some_invalid_visibility"
        };

        Assert.ThrowsAsync<CliException>(async () => await SetPlayerDataItemHandler.SetPlayerDataItemAsync(input, m_MockUnityEnvironment.Object, m_MockCloudSaveDataService.Object, m_MockLogger.Object, default));
    }
}
