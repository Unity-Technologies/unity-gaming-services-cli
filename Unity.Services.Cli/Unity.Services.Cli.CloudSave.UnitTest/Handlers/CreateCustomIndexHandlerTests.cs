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

public class CreateCustomIndexHandlerTests
{
    readonly Mock<ICloudSaveDataService> m_MockCloudSaveDataService = new();
    readonly Mock<IUnityEnvironment> m_MockUnityEnvironment = new();
    readonly Mock<ILogger> m_MockLogger = new();

    static readonly List<IndexField> k_ValidIndexFields = new List<IndexField>()
    {
        new IndexField("key1", true),
        new IndexField("key2", false)
    };

    readonly CreateIndexBody m_ValidCreateCustomIndexBody = new CreateIndexBody(
        new CreateIndexBodyIndexConfig(k_ValidIndexFields));

    readonly CreateIndexResponse m_ValidCreateIndexResponse = new CreateIndexResponse("id", IndexStatus.READY);

    [SetUp]
    public void SetUp()
    {
        m_MockUnityEnvironment.Reset();
        m_MockLogger.Reset();
        m_MockCloudSaveDataService.Reset();
        m_MockCloudSaveDataService.Setup(l =>
                l.CreateCustomIndexAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    CancellationToken.None))
            .Returns(Task.FromResult(m_ValidCreateIndexResponse));
    }

    [Test]
    public async Task CreateCustomIndexHandler_CallsLoadingIndicator()
    {
        Mock<ILoadingIndicator> mockLoadingIndicator = new Mock<ILoadingIndicator>();

        await CreateCustomIndexHandler.CreateCustomIndexAsync(null!, null!, null!, null!, mockLoadingIndicator.Object, CancellationToken.None);

        mockLoadingIndicator.Verify(ex => ex
            .StartLoadingAsync(It.IsAny<string>(), It.IsAny<Func<StatusContext?, Task>>()), Times.Once);
    }

    [Test]
    public void CreateCustomIndexHandler_HandlesInputAndLogsOnSuccess_UsingJsonBody()
    {
        var inputBody = JsonConvert.SerializeObject(m_ValidCreateCustomIndexBody);
        var input = new CreateIndexInput()
        {
            Fields = null,
            JsonFileOrBody = inputBody,
        };
        Assert.DoesNotThrowAsync(async () => await CreateCustomIndexHandler.CreateCustomIndexAsync(input, m_MockUnityEnvironment.Object, m_MockCloudSaveDataService.Object, m_MockLogger.Object, default));
        TestsHelper.VerifyLoggerWasCalled(m_MockLogger, LogLevel.Critical, LoggerExtension.ResultEventId, Times.Once);
    }

    [Test]
    public void CreateCustomIndexHandler_HandlesInputAndLogsOnSuccess_UsingFields()
    {
        var inputFields = JsonConvert.SerializeObject(k_ValidIndexFields);
        var input = new CreateIndexInput()
        {
            Fields = inputFields,
            JsonFileOrBody = null,
        };
        Assert.DoesNotThrowAsync(async () => await CreateCustomIndexHandler.CreateCustomIndexAsync(input, m_MockUnityEnvironment.Object, m_MockCloudSaveDataService.Object, m_MockLogger.Object, default));
        TestsHelper.VerifyLoggerWasCalled(m_MockLogger, LogLevel.Critical, LoggerExtension.ResultEventId, Times.Once);
    }

    [Test]
    public void CreateCustomIndexHandler_MissingBodyThrowsException()
    {
        var input = new CreateIndexInput();
        Assert.ThrowsAsync<CliException>(async () => await CreateCustomIndexHandler.CreateCustomIndexAsync(input, m_MockUnityEnvironment.Object, m_MockCloudSaveDataService.Object, m_MockLogger.Object, default));
    }

    [Test]
    public void CreateCustomIndexAsync_InvalidVisibilityThrowsException()
    {
        var inputFields = JsonConvert.SerializeObject(k_ValidIndexFields);
        var input = new CreateIndexInput()
        {
            Fields = inputFields,
            JsonFileOrBody = null,
            Visibility = "InvalidVisibilityType",
        };
        Assert.ThrowsAsync<CliException>(async () => await CreateCustomIndexHandler.CreateCustomIndexAsync(input, m_MockUnityEnvironment.Object, m_MockCloudSaveDataService.Object, m_MockLogger.Object, default));
    }
}
