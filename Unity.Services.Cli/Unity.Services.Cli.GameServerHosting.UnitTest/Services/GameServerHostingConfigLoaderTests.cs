using Microsoft.Extensions.Logging;
using Moq;
using Unity.Services.Cli.GameServerHosting.Exceptions;
using Unity.Services.Cli.GameServerHosting.Services;
using Unity.Services.Multiplay.Authoring.Core.Assets;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Unity.Services.Cli.GameServerHosting.UnitTest.Services;

public class GameServerHostingConfigLoaderTests
{
    readonly Mock<IDeployFileService> m_MockDeployFileService = new();
    readonly Mock<IMultiplayConfigValidator> m_MockValidator = new();
    readonly Mock<ILogger> m_MockLogger = new();

    readonly GameServerHostingConfigLoader m_Loader;

    readonly MultiplayConfig m_TestFile = new()
    {
        Builds = new Dictionary<BuildName, MultiplayConfig.BuildDefinition>
        {
            {
                new BuildName
                {
                    Name = "test"
                },
                new MultiplayConfig.BuildDefinition()
            }
        }
    };

    public GameServerHostingConfigLoaderTests()
    {
        m_Loader = new GameServerHostingConfigLoader(m_MockDeployFileService.Object, m_MockValidator.Object, m_MockLogger.Object);
    }

    [SetUp]
    public void SetUp()
    {
        m_MockValidator.Reset();
        m_MockLogger.Reset();
        m_MockValidator.Setup(v => v.Validate(It.IsAny<MultiplayConfig>()))
            .Returns(new List<IMultiplayConfigValidator.Error>());
    }

    [Test]
    public async Task Deploy_LoadsSingleConfig()
    {
        SetupInputPaths("test.gsh");
        SetupFiles(new Dictionary<string, MultiplayConfig>
        {
            { "test.gsh", m_TestFile },
        }, CancellationToken.None);

        var config = await m_Loader.LoadAndValidateAsync(new List<string>
        {
            "test.gsh"
        }, CancellationToken.None);

        Assert.That(config.Builds, Contains.Key(new BuildName { Name = "test" }));
    }

    [Test]
    public void Deploy_WhenValidationFails_ThrowsValidationException()
    {
        SetupInputPaths("test.gsh");
        SetupFiles(new Dictionary<string, MultiplayConfig> { { "test.gsh", m_TestFile } }, CancellationToken.None);
        m_MockValidator.Setup(m => m.Validate(It.IsAny<MultiplayConfig>()))
            .Returns(new List<IMultiplayConfigValidator.Error>
            {
                new ("fail")
            });

        Assert.ThrowsAsync<InvalidConfigException>(async () =>
        {
            await m_Loader.LoadAndValidateAsync(new List<string>
            {
                "test.gsh"
            }, CancellationToken.None);
        });
    }

    [Test]
    public async Task Deploy_LoadsMultipleConfigs()
    {
        SetupInputPaths("test.gsh", "test2.gsh");
        SetupFiles(new Dictionary<string, MultiplayConfig>
        {
            { "test.gsh", m_TestFile },
            { @"test2.gsh", new MultiplayConfig
                {
                    Builds = new Dictionary<BuildName, MultiplayConfig.BuildDefinition>
                    {
                        { new BuildName{ Name = "bar" }, new MultiplayConfig.BuildDefinition() }
                    }
                }
            },
        }, CancellationToken.None);

        var config = await m_Loader.LoadAndValidateAsync(new List<string>
        {
            "foo"
        }, CancellationToken.None);

        Assert.That(config.Builds, Contains.Key(new BuildName { Name = "test" }));
        Assert.That(config.Builds, Contains.Key(new BuildName { Name = "bar" }));
    }

    void SetupInputPaths(params string[] paths)
    {
        m_MockDeployFileService.Setup(l => l.ListFilesToDeploy(It.IsAny<List<string>>(), It.IsAny<string>()))
            .Returns(new List<string>(paths));
    }

    void SetupFiles(Dictionary<string, MultiplayConfig> fileSystem, CancellationToken cancellationToken)
    {
        foreach (var (path, config) in fileSystem)
        {
            m_MockDeployFileService.Setup(l => l.ReadAllTextAsync(path, cancellationToken))
                .ReturnsAsync(ConfigFile(config));
        }
    }

    static string ConfigFile(MultiplayConfig config)
    {
        var deserializer = new SerializerBuilder()
            .WithTypeConverter(new ResourceNameTypeConverter())
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();

        return deserializer.Serialize(config);
    }

}
