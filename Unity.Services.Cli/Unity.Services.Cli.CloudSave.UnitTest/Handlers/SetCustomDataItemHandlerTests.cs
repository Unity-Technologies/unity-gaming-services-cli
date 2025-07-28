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
using Unity.Services.Cli.CloudSave.Utils;
using Unity.Services.Cli.Common.Exceptions;
using Unity.Services.Gateway.CloudSaveApiV1.Generated.Model;
using Unity.Services.Cli.TestUtils;

namespace Unity.Services.Cli.CloudSave.UnitTest.Handlers;

public class SetCustomDataItemHandlerTests
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
    public async Task SetCustomDataItemHandler_CallsLoadingIndicator()
    {
        Mock<ILoadingIndicator> mockLoadingIndicator = new Mock<ILoadingIndicator>();

        await SetCustomDataItemHandler.SetCustomDataItemAsync(null!, null!, null!, null!, mockLoadingIndicator.Object, CancellationToken.None);

        mockLoadingIndicator.Verify(ex => ex
            .StartLoadingAsync(It.IsAny<string>(), It.IsAny<Func<StatusContext?, Task>>()), Times.Once);
    }

    [Test]
    public async Task SetCustomDataItemHandler_CallsService_WhenInputIsValid_String()
    {
        SetCustomItemInput input = new()
        {
            CloudProjectId = TestValues.ValidProjectId,
            CustomId = TestValues.ValidCustomId,
            Key = k_ValidKey,
            Value = k_ValidStringValue,
            WriteLock = k_ValidWriteLock,
            Visibility = "private"
        };

        m_MockUnityEnvironment.Setup(x => x.FetchIdentifierAsync(CancellationToken.None))
            .ReturnsAsync(TestValues.ValidEnvironmentId);

        var response = new SetItemResponse(k_ValidWriteLock);

        m_MockCloudSaveDataService.Setup(x => x.SetCustomDataItemAsync(TestValues.ValidProjectId,
            TestValues.ValidEnvironmentId,
            TestValues.ValidCustomId,
            k_ValidKey,
            k_ValidStringValue,
            k_ValidWriteLock,
            CustomIndexVisibilityTypes.Private,
            CancellationToken.None))
            .ReturnsAsync(response);

        await SetCustomDataItemHandler.SetCustomDataItemAsync(
            input,
            m_MockUnityEnvironment.Object,
            m_MockCloudSaveDataService!.Object,
            m_MockLogger!.Object,
            CancellationToken.None
        );

        m_MockUnityEnvironment.Verify(x => x.FetchIdentifierAsync(CancellationToken.None), Times.Once);
        m_MockCloudSaveDataService.Verify(ex => ex.SetCustomDataItemAsync(TestValues.ValidProjectId, TestValues.ValidEnvironmentId,
            TestValues.ValidCustomId, k_ValidKey, k_ValidStringValue, k_ValidWriteLock, CustomIndexVisibilityTypes.Private,
            CancellationToken.None), Times.Once);
    }

    [Test]
    public async Task SetCustomDataItemHandler_CallsService_WhenInputIsValid_Number()
    {
        SetCustomItemInput input = new()
        {
            CloudProjectId = TestValues.ValidProjectId,
            CustomId = TestValues.ValidCustomId,
            Key = k_ValidKey,
            Value = k_ValidNumberValue,
            WriteLock = k_ValidWriteLock,
            Visibility = "private"
        };

        m_MockUnityEnvironment.Setup(x => x.FetchIdentifierAsync(CancellationToken.None))
            .ReturnsAsync(TestValues.ValidEnvironmentId);

        var response = new SetItemResponse(k_ValidWriteLock);

        m_MockCloudSaveDataService.Setup(x => x.SetCustomDataItemAsync(TestValues.ValidProjectId,
                TestValues.ValidEnvironmentId,
                TestValues.ValidCustomId,
                k_ValidKey,
                long.Parse(k_ValidNumberValue),
                k_ValidWriteLock,
                CustomIndexVisibilityTypes.Private,
                CancellationToken.None))
            .ReturnsAsync(response);

        await SetCustomDataItemHandler.SetCustomDataItemAsync(
            input,
            m_MockUnityEnvironment.Object,
            m_MockCloudSaveDataService!.Object,
            m_MockLogger!.Object,
            CancellationToken.None
        );

        m_MockUnityEnvironment.Verify(x => x.FetchIdentifierAsync(CancellationToken.None), Times.Once);
        m_MockCloudSaveDataService.Verify(ex => ex.SetCustomDataItemAsync(TestValues.ValidProjectId, TestValues.ValidEnvironmentId,
            TestValues.ValidCustomId, k_ValidKey, long.Parse(k_ValidNumberValue), k_ValidWriteLock, CustomIndexVisibilityTypes.Private,
            CancellationToken.None), Times.Once);
    }

    [Test]
    public async Task SetCustomDataItemHandler_CallsService_UsesDefaultVisibilityWhenNotSet()
    {
        SetCustomItemInput input = new()
        {
            CloudProjectId = TestValues.ValidProjectId,
            CustomId = TestValues.ValidCustomId,
            Key = k_ValidKey,
            Value = k_ValidStringValue,
            WriteLock = k_ValidWriteLock,
        };

        m_MockUnityEnvironment.Setup(x => x.FetchIdentifierAsync(CancellationToken.None))
            .ReturnsAsync(TestValues.ValidEnvironmentId);

        var response = new SetItemResponse(k_ValidWriteLock);

        m_MockCloudSaveDataService.Setup(x => x.SetCustomDataItemAsync(TestValues.ValidProjectId,
                TestValues.ValidEnvironmentId,
                TestValues.ValidCustomId,
                k_ValidKey,
                k_ValidStringValue,
                k_ValidWriteLock,
                CustomIndexVisibilityTypes.Default,
                CancellationToken.None))
            .ReturnsAsync(response);

        await SetCustomDataItemHandler.SetCustomDataItemAsync(
            input,
            m_MockUnityEnvironment.Object,
            m_MockCloudSaveDataService!.Object,
            m_MockLogger!.Object,
            CancellationToken.None
        );

        m_MockUnityEnvironment.Verify(x => x.FetchIdentifierAsync(CancellationToken.None), Times.Once);
        m_MockCloudSaveDataService.Verify(ex => ex.SetCustomDataItemAsync(TestValues.ValidProjectId, TestValues.ValidEnvironmentId,
            TestValues.ValidCustomId, k_ValidKey, k_ValidStringValue, k_ValidWriteLock, CustomIndexVisibilityTypes.Default,
            CancellationToken.None), Times.Once);
    }

    [Test]
    public void SetCustomDataItemHandler_InvalidVisibilityThrowsException()
    {
        SetCustomItemInput input = new()
        {
            CloudProjectId = TestValues.ValidProjectId,
            CustomId = TestValues.ValidCustomId,
            Key = k_ValidKey,
            Value = k_ValidStringValue,
            Visibility = "some_invalid_visibility"
        };

        Assert.ThrowsAsync<CliException>(async () => await SetCustomDataItemHandler.SetCustomDataItemAsync(input, m_MockUnityEnvironment.Object, m_MockCloudSaveDataService.Object, m_MockLogger.Object, default));
    }
}
