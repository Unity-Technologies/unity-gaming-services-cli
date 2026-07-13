using Moq;
using Newtonsoft.Json;
using NUnit.Framework;
using Unity.Services.Cli.Common.Networking;
using Unity.Services.Cli.Matchmaker.Parser;
using Unity.Services.Cli.Matchmaker.Service;
using Unity.Services.Cli.Matchmaker.UnitTest.SampleConfigs;
using Unity.Services.Multiplayer.Editor.Matchmaker.Authoring.Core.ConfigApi;
using Core = Unity.Services.Multiplayer.Editor.Matchmaker.Authoring.Core.Model;
using Generated = Unity.Services.Gateway.MatchmakerAdminApiV3.Generated.Model;


namespace Unity.Services.Cli.Matchmaker.UnitTest;

[TestFixture]
class AdminApiClientUnitTests
{
    [SetUp]
    public void Setup()
    {
        EndpointHelper.InitializeNetworkTargetEndpoints(
        [
            new AdminApiTargetEndpoint(),
            new UnityServicesGatewayEndpoints()
        ]);
    }

    [Test]
    public async Task GetEnvironmentConfigNotFound()
    {
        var configService = new Mock<IMatchmakerService>();
        configService
            .Setup(x => x.GetEnvironmentConfig(default))
            .Returns(Task.FromResult((false, new Generated.EnvironmentConfig())));

        var client = new AdminApiClient.MatchmakerAdminClient(configService.Object);

        await client.Initialize(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), default);
        var (exist, _) = await client.GetEnvironmentConfig(default);

        Assert.That(exist, Is.EqualTo(false));
    }

    [Test]
    public async Task GetEnvironmentConfig()
    {
        var removeEnvConfig = new Generated.EnvironmentConfig()
        {
            Enabled = true,
            DefaultQueueName = "Test"
        };
        var configService = new Mock<IMatchmakerService>();
        configService
            .Setup(x => x.GetEnvironmentConfig(default))
            .Returns(Task.FromResult((true, removeEnvConfig)));
        var expectedConfig = new Core.EnvironmentConfig
        {
            Enabled = true,
            DefaultQueueName = new Core.QueueName("Test")
        };

        var client = new AdminApiClient.MatchmakerAdminClient(configService.Object);

        await client.Initialize(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), default);
        var actualConfig = await client.GetEnvironmentConfig(default);

        var actualJson = JsonConvert.SerializeObject(actualConfig.Item2, MatchmakerConfigParser.JsonSerializerSettings);
        var expectedJson = JsonConvert.SerializeObject(expectedConfig, MatchmakerConfigParser.JsonSerializerSettings);
        Assert.That(actualJson, Is.EqualTo(expectedJson));
    }

    [Test]
    public async Task UpsertEnvironmentConfig()
    {
        var configService = new Mock<IMatchmakerService>();
        configService
            .Setup(x => x.UpsertEnvironmentConfig(It.IsAny<Generated.EnvironmentConfig>(), false, default))
            .Returns(Task.FromResult(new List<Core.ErrorResponse>() { new() { ResultCode = "MockedFailedValidation", Message = "Mocked failed validation" } }));
        var localConfig = new Core.EnvironmentConfig
        {
            Enabled = true,
            DefaultQueueName = new Core.QueueName("Test")
        };
        var client = new AdminApiClient.MatchmakerAdminClient(configService.Object);
        await client.Initialize(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), default);
        var errors = await client.UpsertEnvironmentConfig(localConfig, false, default);

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
        var coreSampleConfig = new CoreSampleConfig();
        var configService = new Mock<IMatchmakerService>();
        configService
            .Setup(x => x.ListQueues(default))
            .Returns(Task.FromResult(new List<Generated.QueueConfig>()
            {
                GeneratedSampleConfig.QueueConfig,
                GeneratedSampleConfig.EmptyQueueConfig
            }));
        var expectedQueueConfigs = new List<Core.QueueConfig>()
        {
            coreSampleConfig.QueueConfig,
            coreSampleConfig.EmptyQueueConfig
        };

        var client = new AdminApiClient.MatchmakerAdminClient(configService.Object);

        await client.Initialize(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), default);
        var actualConfig = await client.ListQueues(default);

        Assert.That(actualConfig.Count, Is.EqualTo(2));
        Assert.That(actualConfig[0].Item2, Is.Empty);
        Assert.That(actualConfig[1].Item2, Is.Empty);
        var actualJson = JsonConvert.SerializeObject(actualConfig[0].Item1, MatchmakerConfigParser.JsonSerializerSettings);
        var expectedJson = JsonConvert.SerializeObject(expectedQueueConfigs[0], MatchmakerConfigParser.JsonSerializerSettings);
        Assert.That(expectedJson, Is.EqualTo(actualJson));
        actualJson = JsonConvert.SerializeObject(actualConfig[1].Item1, MatchmakerConfigParser.JsonSerializerSettings);
        expectedJson = JsonConvert.SerializeObject(expectedQueueConfigs[1], MatchmakerConfigParser.JsonSerializerSettings);
        Assert.That(expectedJson, Is.EqualTo(actualJson));

        var defaultPoolHosting = actualConfig[0].Item1.DefaultPool!.MatchHosting;
        Assert.That(defaultPoolHosting, Is.TypeOf<Core.MatchIdConfig>());

        Assert.That(actualConfig[0].Item1.FilteredPools!.Count, Is.EqualTo(2));

        var filtered0 = actualConfig[0].Item1.FilteredPools![0].MatchHosting;
        var filtered1 = actualConfig[0].Item1.FilteredPools![1].MatchHosting;

        Assert.That(filtered0, Is.TypeOf<Core.MatchIdConfig>());
        Assert.That(filtered1, Is.TypeOf<Core.CloudCodeConfig>());

        var cloudCodeHosting = (Core.CloudCodeConfig)filtered1;
        Assert.That(cloudCodeHosting.ModuleName, Is.EqualTo("cc-module"));
        Assert.That(cloudCodeHosting.AllocateFunctionName, Is.EqualTo("cc-allocate"));
        Assert.That(cloudCodeHosting.PollFunctionName, Is.EqualTo("cc-poll"));
    }

    [Test]
    [TestCase(true)]
    [TestCase(false)]
    public async Task UpsertQueue(bool emptyQueue)
    {
        var coreSampleConfig = new CoreSampleConfig();
        var configService = new Mock<IMatchmakerService>();
        configService
            .Setup(x => x.UpsertQueueConfig(It.IsAny<Generated.QueueConfig>(), false, default))
            .Returns(Task.FromResult(new List<Core.ErrorResponse>() { new() { ResultCode = "MockedFailedValidation", Message = "Mocked failed validation" } }));

        var client = new AdminApiClient.MatchmakerAdminClient(configService.Object);

        await client.Initialize(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), default);
        var errors = await client.UpsertQueue(emptyQueue ? coreSampleConfig.EmptyQueueConfig : coreSampleConfig.QueueConfig, new Core.MultiplayResources(), false);

        Assert.That(configService.Invocations.Count, Is.EqualTo(2));
        var that = configService.Invocations[1].Arguments[0];
        var actualEnvConfig = JsonConvert.SerializeObject(that, MatchmakerConfigParser.JsonSerializerSettings);
        var expectedConfig = JsonConvert.SerializeObject(emptyQueue ? GeneratedSampleConfig.EmptyQueueConfig : GeneratedSampleConfig.QueueConfig, MatchmakerConfigParser.JsonSerializerSettings);
        Assert.That(actualEnvConfig, Is.EqualTo(expectedConfig));
        Assert.That(errors.Count, Is.EqualTo(1));
        Assert.That(errors[0].ResultCode, Is.EqualTo("MockedFailedValidation"));
    }

    [Test]
    public async Task UpsertQueueWithMultiplayConfigReturnsError()
    {
        var configService = new Mock<IMatchmakerService>();
        var client = new AdminApiClient.MatchmakerAdminClient(configService.Object);
        await client.Initialize(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), default);

        var queue = new Core.QueueConfig
        {
            Name = new Core.QueueName("TestQueue"),
            Enabled = true,
            MaxPlayersPerTicket = 1,
            DefaultPool = new Core.BasePoolConfig
            {
                Name = new Core.PoolName("TestPool"),
                Enabled = true,
                MatchHosting = new Core.MultiplayConfig
                {
                    FleetName = "SomeFleet",
                    BuildConfigurationName = "SomeBuild",
                    DefaultQoSRegionName = "SomeRegion"
                },
                MatchLogic = new Core.MatchLogicRulesConfig
                {
                    Name = "logic",
                    MatchDefinition = new Core.RuleBasedMatchDefinition()
                }
            }
        };

        var errors = await client.UpsertQueue(queue, new Core.MultiplayResources(), false);

        Assert.That(errors.Count, Is.EqualTo(1));
        Assert.That(errors[0].ResultCode, Is.EqualTo("UnsupportedMultiplayHosting"));
    }

    [Test]
    public async Task ListQueuesWithMultiplayPoolReturnsError()
    {
        var configService = new Mock<IMatchmakerService>();
        configService.Setup(f => f.ListQueues(default))
            .ReturnsAsync(new List<Generated.QueueConfig>()
            {
                new Generated.QueueConfig(
                    name: "TestQueue",
                    enabled: true,
                    maxPlayersPerTicket: 1,
                    defaultPool: new Generated.BasePoolConfig(
                        name: "TestPool",
                        enabled: true,
                        matchHosting: new Generated.MatchHosting(
                            new Generated.MultiplayHostingConfig(
                                type: Generated.MultiplayHostingConfig.TypeEnum.Multiplay,
                                fleetId: "some-fleet-id",
                                buildConfigurationId: "some-build-id",
                                defaultQoSRegionId: "some-region-id"
                            )),
                        matchLogic: new Generated.Rules(
                            name: "logic",
                            backfillEnabled: false,
                            matchDefinition: new Generated.RuleBasedMatchDefinition(
                                matchRules: new List<Generated.Rule>(),
                                teams: new List<Generated.RuleBasedTeamDefinition>()
                            )),
                        variants: new List<Generated.PoolConfig>()
                    ),
                    filteredPools: new List<Generated.FilteredPoolConfig>()
                )
            });

        var client = new AdminApiClient.MatchmakerAdminClient(configService.Object);
        await client.Initialize(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), default);

        var response = await client.ListQueues();

        Assert.That(response.Count, Is.EqualTo(1));
        Assert.That(response[0].Item2.Count, Is.EqualTo(1));
        Assert.That(response[0].Item2[0].ResultCode, Is.EqualTo("UnsupportedMultiplayHosting"));
    }

    [Test]
    public async Task DeleteQueue()
    {
        var configService = new Mock<IMatchmakerService>();
        configService
            .Setup(x => x.DeleteQueue("ToDelete", false, default))
            .Returns(Task.FromResult(new List<Core.ErrorResponse>()));

        var client = new AdminApiClient.MatchmakerAdminClient(configService.Object);

        await client.DeleteQueue(new Core.QueueName("ToDelete"), false);

        Assert.That(configService.Invocations.Count, Is.EqualTo(1));
        var name = configService.Invocations[0].Arguments[0];
        Assert.That(name, Is.EqualTo("ToDelete"));
    }

    [Test]
    public void GetRemoteMultiplayResourcesReturnsEmpty()
    {
        var configService = new Mock<IMatchmakerService>();
        var client = new AdminApiClient.MatchmakerAdminClient(configService.Object);

        var resources = ((IConfigApiClient)client).GetRemoteMultiplayResources();

        Assert.That(resources, Is.Not.Null);
        Assert.That(resources.Fleets, Is.Null.Or.Empty);
    }
}
