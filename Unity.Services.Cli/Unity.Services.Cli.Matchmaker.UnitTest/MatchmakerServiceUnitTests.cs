using System.Net;
using KellermanSoftware.CompareNetObjects;
using Moq;
using NUnit.Framework;
using Unity.Services.Cli.Common.Validator;
using Unity.Services.Cli.Matchmaker.Service;
using Unity.Services.Cli.Matchmaker.UnitTest.SampleConfigs;
using Unity.Services.Cli.ServiceAccountAuthentication;
using Unity.Services.Gateway.MatchmakerAdminApiV3.Generated.Api;
using Unity.Services.Gateway.MatchmakerAdminApiV3.Generated.Client;
using Generated = Unity.Services.Gateway.MatchmakerAdminApiV3.Generated.Model;

namespace Unity.Services.Cli.Matchmaker.UnitTest;

[TestFixture]
public class MatchmakerServiceTests
{
    Mock<IMatchmakerAdminApi> m_MockMatchmakerAdminApi = null!;
    Mock<IServiceAccountAuthenticationService> m_MockAuthService = null!;
    Mock<IConfigurationValidator> m_MockConfigValidator = null!;
    MatchmakerService m_Service = null!;

    CompareLogic m_CompareLogic = new CompareLogic();

    [SetUp]
    public void Setup()
    {
        m_CompareLogic.Config.ComparePrivateProperties = true;
        m_CompareLogic.Config.ComparePrivateFields = true;
        m_MockMatchmakerAdminApi = new Mock<IMatchmakerAdminApi>();
        m_MockAuthService = new Mock<IServiceAccountAuthenticationService>();
        m_MockConfigValidator = new Mock<IConfigurationValidator>();
        m_Service = new MatchmakerService(m_MockMatchmakerAdminApi.Object, m_MockAuthService.Object, m_MockConfigValidator.Object);
    }

    [Test]
    public async Task Initialize_SetsProjectAndEnvironmentIds()
    {
        // Arrange
        string projectId = "testProjectId";
        string environmentId = "testEnvironmentId";
        m_MockAuthService.Setup(x => x.GetAccessTokenAsync(It.IsAny<CancellationToken>())).ReturnsAsync("testToken");
        var mockHeaders = new Mock<IDictionary<string, string>>(); // Mock the DefaultHeaders property
        m_MockMatchmakerAdminApi.Setup(x => x.Configuration.DefaultHeaders).Returns(mockHeaders.Object); // Setup the mock

        string actualKey = "";
        string actualValue = "";
        mockHeaders.SetupSet(x => x[It.IsAny<string>()] = It.IsAny<string>())
            .Callback<string, string>((k, v) => { actualKey = k; actualValue = v; });

        // Act
        await m_Service.Initialize(projectId, environmentId);

        // Assert
        m_MockAuthService.Verify(x => x.GetAccessTokenAsync(It.IsAny<CancellationToken>()), Times.Once);
        Assert.That(actualKey, Is.EqualTo("Authorization"));
        Assert.That(actualValue, Is.EqualTo("Basic testToken"));
    }

    [Test]
    public async Task GetEnvironmentConfig_ReturnsEnvironmentConfig()
    {
        // Arrange
        var expectedConfig = new Generated.EnvironmentConfig { Enabled = true, DefaultQueueName = "TestQueue" };
        m_MockMatchmakerAdminApi.Setup(x => x.GetEnvironmentConfigWithHttpInfoAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ApiResponse<Generated.EnvironmentConfig>(HttpStatusCode.OK, expectedConfig));

        // Act
        var (exists, config) = await m_Service.GetEnvironmentConfig();

        // Assert
        Assert.IsTrue(exists);
        Assert.That(config, Is.EqualTo(expectedConfig));
    }

    [Test]
    public async Task GetNonExistingEnvironmentConfig_ReturnsEmptyEnvironmentConfig()
    {
        // Arrange
        m_MockMatchmakerAdminApi.Setup(x => x.GetEnvironmentConfigWithHttpInfoAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Throws(new ApiException(404, "Not found"));

        // Act
        var (exists, _) = await m_Service.GetEnvironmentConfig();

        // Assert
        Assert.IsFalse(exists);
    }

    [Test]
    public async Task UpsertEnvironmentConfig_CallsUpdateEnvironmentConfigWithCorrectParameters()
    {
        // Arrange
        var environmentConfig = new Generated.EnvironmentConfig { Enabled = true, DefaultQueueName = "TestQueue" };
        m_MockMatchmakerAdminApi.Setup(x => x.UpdateEnvironmentConfigWithHttpInfoAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<Generated.EnvironmentConfig>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ApiResponse<Object>(HttpStatusCode.OK, new Object()));

        // Act
        await m_Service.UpsertEnvironmentConfig(environmentConfig, false);

        // Assert
        m_MockMatchmakerAdminApi.Verify(x => x.UpdateEnvironmentConfigWithHttpInfoAsync(It.IsAny<string>(), It.IsAny<string>(), false, environmentConfig, It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task UpsertEnvironmentConfig_CallsUpdateEnvironmentConfigWithInvalidParameters()
    {
        // Arrange
        var environmentConfig = new Generated.EnvironmentConfig { Enabled = true, DefaultQueueName = "InvalidQueueName" };
        var problemDetails = new Generated.ProblemDetails()
        {
            Detail = "Mocked Error",
            Details = new List<Generated.ProblemDetailsDetailsInner>()
            {
                new Generated.ProblemDetailsDetailsInner()
                {
                    ResultCode = "MockedError",
                    Message = "Mocked Error Message"
                }
            }
        };
        m_MockMatchmakerAdminApi.Setup(
                x => x.UpdateEnvironmentConfigWithHttpInfoAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<bool>(),
                    It.IsAny<Generated.EnvironmentConfig>(),
                    It.IsAny<int>(),
                    It.IsAny<CancellationToken>()))
            .Throws(new ApiException(400, "Mocked error", problemDetails.ToJson()));

        // Act
        var errors = await m_Service.UpsertEnvironmentConfig(environmentConfig, false);

        // Assert
        m_MockMatchmakerAdminApi.Verify(x => x.UpdateEnvironmentConfigWithHttpInfoAsync(It.IsAny<string>(), It.IsAny<string>(), false, environmentConfig, It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);
        Assert.That(errors.Count, Is.EqualTo(1));
        Assert.That(errors.First().ResultCode, Is.EqualTo("MockedError"));
        Assert.That(errors.First().Message, Is.EqualTo("Mocked Error Message"));
    }

    [Test]
    public async Task ListQueues_ReturnsListOfQueues()
    {
        // Arrange
        var generatedSampleConfig = new GeneratedSampleConfig();
        var expectedQueues = new List<Generated.QueueConfig> { generatedSampleConfig.QueueConfig };
        m_MockMatchmakerAdminApi.Setup(x => x.ListQueuesWithHttpInfoAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ApiResponse<List<Generated.QueueConfig>>(HttpStatusCode.OK, expectedQueues, $"[{generatedSampleConfig.QueueConfig}]"));

        // Act
        var queues = await m_Service.ListQueues();

        // Assert
        var compResult = m_CompareLogic.Compare(generatedSampleConfig.QueueConfig, queues.First());
        Assert.IsTrue(compResult.AreEqual, compResult.DifferencesString);
        Assert.That(queues, Is.EqualTo(expectedQueues));
    }

    [Test]
    public async Task ListEmptyQueues_ReturnsEmptyListOfQueues()
    {
        // Arrange
        m_MockMatchmakerAdminApi.Setup(x => x.ListQueuesWithHttpInfoAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Throws(new ApiException(404, "Not found"));

        // Act
        var queues = await m_Service.ListQueues();

        // Assert
        Assert.That(queues, Is.Empty);
    }

    [Test]
    public async Task UpsertQueueConfig_CallsUpsertQueueConfigWithCorrectParameters()
    {
        // Arrange
        var generatedSampleConfig = new GeneratedSampleConfig();
        var queueConfig = generatedSampleConfig.QueueConfig;
        m_MockMatchmakerAdminApi.Setup(x => x.UpsertQueueConfigWithHttpInfoAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<Generated.QueueConfig>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ApiResponse<Object>(HttpStatusCode.OK, new Object()));

        // Act
        await m_Service.UpsertQueueConfig(queueConfig, false);

        // Assert
        m_MockMatchmakerAdminApi.Verify(x => x.UpsertQueueConfigWithHttpInfoAsync(It.IsAny<string>(), It.IsAny<string>(), queueConfig.Name, false, queueConfig, It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task UpsertQueue_CallsUpdateQueueWithInvalidParameters()
    {
        // Arrange
        var queueConfig = new Generated.QueueConfig("MyQueue");
        var problemDetails = new Generated.ProblemDetails()
        {
            Detail = "Mocked Error",
            Details = new List<Generated.ProblemDetailsDetailsInner>()
            {
                new Generated.ProblemDetailsDetailsInner()
                {
                    ResultCode = "MockedError",
                    Message = "Mocked Error Message"
                }
            }
        };
        m_MockMatchmakerAdminApi.Setup(
                x => x.UpsertQueueConfigWithHttpInfoAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    "MyQueue",
                    false,
                    It.IsAny<Generated.QueueConfig>(),
                    It.IsAny<int>(),
                    It.IsAny<CancellationToken>()
                    ))
            .Throws(new ApiException(400, "Mocked error", problemDetails.ToJson()));

        // Act
        var errors = await m_Service.UpsertQueueConfig(queueConfig, false);

        // Assert
        m_MockMatchmakerAdminApi.Verify(x => x.UpsertQueueConfigWithHttpInfoAsync(It.IsAny<string>(), It.IsAny<string>(), "MyQueue", false, It.IsAny<Generated.QueueConfig>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);
        Assert.That(errors.Count, Is.EqualTo(1));
        Assert.That(errors.First().ResultCode, Is.EqualTo("MockedError"));
        Assert.That(errors.First().Message, Is.EqualTo("Mocked Error Message"));
    }

    [Test]
    public async Task DeleteQueue_CallsDeleteQueueWithCorrectParameters()
    {
        // Arrange
        string queueName = "TestQueue";
        m_MockMatchmakerAdminApi.Setup(x => x.DeleteQueueAsync(It.IsAny<string>(), It.IsAny<string>(), queueName, It.IsAny<bool>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await m_Service.DeleteQueue(queueName, false);

        // Assert
        m_MockMatchmakerAdminApi.Verify(x => x.DeleteQueueAsync(It.IsAny<string>(), It.IsAny<string>(), queueName, false, It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
