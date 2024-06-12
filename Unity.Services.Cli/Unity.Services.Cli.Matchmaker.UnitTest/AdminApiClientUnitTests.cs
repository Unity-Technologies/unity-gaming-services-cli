using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Newtonsoft.Json;
using NUnit.Framework;
using Unity.Services.Cli.Common.Networking;
using Unity.Services.Cli.GameServerHosting.Service;
using Unity.Services.Cli.Matchmaker.Parser;
using Unity.Services.Cli.Matchmaker.Service;
using Unity.Services.Cli.Matchmaker.UnitTest.SampleConfigs;
using Unity.Services.Cli.ServiceAccountAuthentication;
using Unity.Services.Matchmaker.Authoring.Core.ConfigApi;
using Core = Unity.Services.Matchmaker.Authoring.Core.Model;
using Generated = Unity.Services.Gateway.MatchmakerAdminApiV3.Generated.Model;


namespace Unity.Services.Cli.Matchmaker.UnitTest;

[TestFixture]
class AdminApiClientUnitTests
{
    Mock<IServiceAccountAuthenticationService> m_MockSaAuthService = null!;

    [SetUp]
    public void Setup()
    {
        var types = new List<TypeInfo>
        {
            typeof(AdminApiTargetEndpoint).GetTypeInfo(),
            typeof(UnityServicesGatewayEndpoints).GetTypeInfo(),
        };
        EndpointHelper.InitializeNetworkTargetEndpoints(types);
        m_MockSaAuthService = new Mock<IServiceAccountAuthenticationService>();
        m_MockSaAuthService.Setup(x => x.GetAccessTokenAsync(default)).Returns(Task.FromResult("token"));
        var configClient = new Mock<IConfigApiClient>();
        new ServiceCollection()
            .AddSingleton(m_MockSaAuthService.Object)
            .AddSingleton(configClient.Object)
            .BuildServiceProvider();
    }

    [Test]
    public async Task GetEnvironmentConfigNotFound()
    {
        // Setup
        var multiplaySampleConfig = new MultiplaySampleConfig();
        var gshService = new Mock<IGameServerHostingService>();
        gshService.Setup(f => f.FleetsApi.ListFleets(It.IsAny<Guid>(), It.IsAny<Guid>(), default)).Returns(multiplaySampleConfig.RemoteFleets);
        var configService = new Mock<IMatchmakerService>();
        configService
            .Setup(x => x.GetEnvironmentConfig(default))
            .Returns(Task.FromResult((false, new Generated.EnvironmentConfig())));

        var client = new AdminApiClient.MatchmakerAdminClient(configService.Object, gshService.Object);

        // Test
        await client.Initialize(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), default);
        var (exist, _) = await client.GetEnvironmentConfig(default);

        // Assert
        Assert.That(exist, Is.EqualTo(false));
    }

    [Test]
    public async Task GetEnvironmentConfig()
    {
        // Setup
        var multiplaySampleConfig = new MultiplaySampleConfig();
        var removeEnvConfig = new Generated.EnvironmentConfig()
        {
            Enabled = true,
            DefaultQueueName = "Test"
        };
        var gshService = new Mock<IGameServerHostingService>();
        gshService.Setup(f => f.FleetsApi.ListFleets(It.IsAny<Guid>(), It.IsAny<Guid>(), default)).Returns(multiplaySampleConfig.RemoteFleets);
        var configService = new Mock<IMatchmakerService>();
        configService
            .Setup(x => x.GetEnvironmentConfig(default))
            .Returns(Task.FromResult((true, removeEnvConfig)));
        var expectedConfig = new Core.EnvironmentConfig
        {
            Type = Core.IMatchmakerConfig.ConfigType.EnvironmentConfig,
            Enabled = true,
            DefaultQueueName = new Core.QueueName("Test")
        };

        var client = new AdminApiClient.MatchmakerAdminClient(configService.Object, gshService.Object);

        // Test
        await client.Initialize(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), default);
        var actualConfig = await client.GetEnvironmentConfig(default);

        // Assert
        var actualJson = JsonConvert.SerializeObject(actualConfig.Item2, MatchmakerConfigParser.JsonSerializerSettings);
        var expectedJson = JsonConvert.SerializeObject(expectedConfig, MatchmakerConfigParser.JsonSerializerSettings);
        Assert.That(actualJson, Is.EqualTo(expectedJson));
    }

    [Test]
    public async Task UpsertEnvironmentConfig()
    {
        // Setup
        var multiplaySampleConfig = new MultiplaySampleConfig();
        var gshService = new Mock<IGameServerHostingService>();
        gshService.Setup(f => f.FleetsApi.ListFleets(It.IsAny<Guid>(), It.IsAny<Guid>(), default)).Returns(multiplaySampleConfig.RemoteFleets);
        var configService = new Mock<IMatchmakerService>();
        configService
            .Setup(x => x.UpsertEnvironmentConfig(It.IsAny<Generated.EnvironmentConfig>(), false, default))
            .Returns(Task.FromResult(new List<Core.ErrorResponse>() { new() { ResultCode = "MockedFailedValidation", Message = "Mocked failed validation" } }));
        var localConfig = new Core.EnvironmentConfig
        {
            Enabled = true,
            DefaultQueueName = new Core.QueueName("Test")
        };
        var client = new AdminApiClient.MatchmakerAdminClient(configService.Object, gshService.Object);
        await client.Initialize(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), default);
        var errors = await client.UpsertEnvironmentConfig(localConfig, false, default);

        // Assert
        Assert.That(configService.Invocations.Count, Is.EqualTo(2));
        var actualEnvConfig = JsonConvert.SerializeObject(configService.Invocations[1].Arguments[0]);
        var expectedConfig = JsonConvert.SerializeObject(new Generated.EnvironmentConfig()
        {
            Enabled = true,
            DefaultQueueName = "Test"
        });
        Assert.That(actualEnvConfig, Is.EqualTo(expectedConfig));
        Assert.That(errors.Count, Is.EqualTo(1));
        Assert.That(errors[0].ResultCode, Is.EqualTo("MockedFailedValidation"));
    }


    [Test]
    public async Task ListQueues()
    {
        // Setup
        var multiplaySampleConfig = new MultiplaySampleConfig();
        var coreSampleConfig = new CoreSampleConfig();
        var generatedSampleConfig = new GeneratedSampleConfig();
        var gshService = new Mock<IGameServerHostingService>();
        gshService.Setup(f => f.FleetsApi.ListFleets(It.IsAny<Guid>(), It.IsAny<Guid>(), default)).Returns(multiplaySampleConfig.RemoteFleets);
        var configService = new Mock<IMatchmakerService>();
        configService
            .Setup(x => x.ListQueues(default))
            .Returns(Task.FromResult(new List<Generated.QueueConfig>()
            {
                generatedSampleConfig.QueueConfig,
                generatedSampleConfig.EmptyQueueConfig
            }));
        var expectedQueueConfigs = new List<Core.QueueConfig>()
        {
            coreSampleConfig.QueueConfig,
            coreSampleConfig.EmptyQueueConfig
        };

        var client = new AdminApiClient.MatchmakerAdminClient(configService.Object, gshService.Object);

        // Test
        await client.Initialize(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), default);
        var actualConfig = await client.ListQueues(default);

        // Assert
        Assert.That(actualConfig.Count, Is.EqualTo(2));
        Assert.That(actualConfig[0].Item2, Is.Empty);
        Assert.That(actualConfig[1].Item2, Is.Empty);
        var actualJson = JsonConvert.SerializeObject(actualConfig[0].Item1, MatchmakerConfigParser.JsonSerializerSettings);
        var expectedJson = JsonConvert.SerializeObject(expectedQueueConfigs[0], MatchmakerConfigParser.JsonSerializerSettings);
        Assert.That(expectedJson, Is.EqualTo(actualJson));
        actualJson = JsonConvert.SerializeObject(actualConfig[1].Item1, MatchmakerConfigParser.JsonSerializerSettings);
        expectedJson = JsonConvert.SerializeObject(expectedQueueConfigs[1], MatchmakerConfigParser.JsonSerializerSettings);
        Assert.That(expectedJson, Is.EqualTo(actualJson));
    }

    [Test]
    [TestCase(true)]
    [TestCase(false)]
    public async Task UpsertQueue(bool emptyQueue)
    {
        // Setup
        var multiplaySampleConfig = new MultiplaySampleConfig();
        var coreSampleConfig = new CoreSampleConfig();
        var generatedSampleConfig = new GeneratedSampleConfig();
        var gshService = new Mock<IGameServerHostingService>();
        gshService.Setup(f => f.FleetsApi.ListFleets(It.IsAny<Guid>(), It.IsAny<Guid>(), default)).Returns(multiplaySampleConfig.RemoteFleets);
        var configService = new Mock<IMatchmakerService>();
        configService
            .Setup(x => x.UpsertQueueConfig(It.IsAny<Generated.QueueConfig>(), false, default))
            .Returns(Task.FromResult(new List<Core.ErrorResponse>() { new() { ResultCode = "MockedFailedValidation", Message = "Mocked failed validation" } }));

        var client = new AdminApiClient.MatchmakerAdminClient(configService.Object, gshService.Object);

        // Test
        await client.Initialize(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), default);
        var errors = await client.UpsertQueue(emptyQueue ? coreSampleConfig.EmptyQueueConfig : coreSampleConfig.QueueConfig, multiplaySampleConfig.LocalResources, false);

        // Assert
        Assert.That(configService.Invocations.Count, Is.EqualTo(2));
        var that = configService.Invocations[1].Arguments[0];
        var actualEnvConfig = JsonConvert.SerializeObject(that, MatchmakerConfigParser.JsonSerializerSettings);
        var expectedConfig = JsonConvert.SerializeObject(emptyQueue ? generatedSampleConfig.EmptyQueueConfig : generatedSampleConfig.QueueConfig, MatchmakerConfigParser.JsonSerializerSettings);
        Assert.That(actualEnvConfig, Is.EqualTo(expectedConfig));
        Assert.That(errors.Count, Is.EqualTo(1));
        Assert.That(errors[0].ResultCode, Is.EqualTo("MockedFailedValidation"));
    }

    [Test]
    public async Task UpsertQueueInvalidMultiplayConfig()
    {
        // Setup
        var multiplaySampleConfig = new MultiplaySampleConfig();
        var coreSampleConfig = new CoreSampleConfig();
        var gshService = new Mock<IGameServerHostingService>();
        gshService.Setup(f => f.FleetsApi.ListFleets(It.IsAny<Guid>(), It.IsAny<Guid>(), default)).Returns(multiplaySampleConfig.RemoteFleets);
        var client = new AdminApiClient.MatchmakerAdminClient(new Mock<IMatchmakerService>().Object, gshService.Object);
        var queue = coreSampleConfig.QueueConfig;
        await client.Initialize(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), default);

        // Test
        var multiplayConfig = (Core.MultiplayConfig)queue.DefaultPool.MatchHosting;
        multiplayConfig.DefaultQoSRegionName = "Invalid";
        var errors = await client.UpsertQueue(coreSampleConfig.QueueConfig, multiplaySampleConfig.LocalResources, false);

        // Assert
        Assert.That(errors.Count, Is.EqualTo(1));
        Assert.That(errors[0].ResultCode, Is.EqualTo("InvalidDefaultQoSRegion"));

        // Test
        multiplayConfig.BuildConfigurationName = "Invalid";
        errors = await client.UpsertQueue(coreSampleConfig.QueueConfig, multiplaySampleConfig.LocalResources, false);

        // Assert
        Assert.That(errors.Count, Is.EqualTo(1));
        Assert.That(errors[0].ResultCode, Is.EqualTo("InvalidBuildConfigurationName"));

        // Test
        multiplayConfig.FleetName = "Invalid";
        errors = await client.UpsertQueue(coreSampleConfig.QueueConfig, multiplaySampleConfig.LocalResources, false);

        // Assert
        Assert.That(errors.Count, Is.EqualTo(1));
        Assert.That(errors[0].ResultCode, Is.EqualTo("InvalidMultiplayFleetName"));
    }


    [Test]
    public async Task GetQueueInvalidMultiplayConfig()
    {
        // Setup
        var multiplaySampleConfig = new MultiplaySampleConfig();
        var generatedSampleConfig = new GeneratedSampleConfig();

        var gshService = new Mock<IGameServerHostingService>();
        var configService = new Mock<IMatchmakerService>();
        var multiplayConfig = multiplaySampleConfig.RemoteFleets;
        configService.Setup(f => f.ListQueues(default))
            .ReturnsAsync(new List<Generated.QueueConfig>() { generatedSampleConfig.QueueConfig });
        var client = new AdminApiClient.MatchmakerAdminClient(configService.Object, gshService.Object);

        // Setup
        multiplayConfig[0].Regions[0].RegionID = new Guid();
        gshService.Setup(f => f.FleetsApi.ListFleets(It.IsAny<Guid>(), It.IsAny<Guid>(), default)).Returns(multiplayConfig);
        await client.Initialize(Guid.NewGuid().ToString(), Guid.NewGuid().ToString());

        // Test
        var response = await client.ListQueues();

        // Assert
        Assert.That(response.Count, Is.EqualTo(1));
        Assert.That(response[0].Item2.Count, Is.EqualTo(1));
        Assert.That(response[0].Item2[0].ResultCode, Is.EqualTo("InvalidDefaultQoSRegion"));

        // Setup
        multiplayConfig[0].BuildConfigurations[0].Id = 0;
        gshService.Setup(f => f.FleetsApi.ListFleets(It.IsAny<Guid>(), It.IsAny<Guid>(), default)).Returns(multiplayConfig);
        await client.Initialize(Guid.NewGuid().ToString(), Guid.NewGuid().ToString());

        // Test
        response = await client.ListQueues();

        // Assert
        Assert.That(response.Count, Is.EqualTo(1));
        Assert.That(response[0].Item2.Count, Is.EqualTo(1));
        Assert.That(response[0].Item2[0].ResultCode, Is.EqualTo("InvalidBuildConfigurationId"));

        // Setup
        multiplayConfig[0].Id = new Guid();
        gshService.Setup(f => f.FleetsApi.ListFleets(It.IsAny<Guid>(), It.IsAny<Guid>(), default)).Returns(multiplayConfig);
        await client.Initialize(Guid.NewGuid().ToString(), Guid.NewGuid().ToString());

        // Test
        response = await client.ListQueues();

        // Assert
        Assert.That(response.Count, Is.EqualTo(1));
        Assert.That(response[0].Item2.Count, Is.EqualTo(1));
        Assert.That(response[0].Item2[0].ResultCode, Is.EqualTo("InvalidMultiplayFleetId"));
    }

    [Test]
    public async Task DeleteQueue()
    {
        // Setup
        var configService = new Mock<IMatchmakerService>();
        configService
            .Setup(x => x.DeleteQueue("ToDelete", false, default))
            .Returns(Task.FromResult(new List<Core.ErrorResponse>()));

        var client = new AdminApiClient.MatchmakerAdminClient(configService.Object, new Mock<IGameServerHostingService>().Object);

        // Test
        await client.DeleteQueue(new Core.QueueName("ToDelete"), false);

        // Assert
        Assert.That(configService.Invocations.Count, Is.EqualTo(1));
        var name = configService.Invocations[0].Arguments[0];
        Assert.That(name, Is.EqualTo("ToDelete"));
    }

}
