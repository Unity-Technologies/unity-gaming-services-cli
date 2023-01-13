using Moq;
using Newtonsoft.Json;
using NUnit.Framework;
using Unity.Services.Cli.Common.Exceptions;
using Unity.Services.Cli.RemoteConfig.Deploy;
using Unity.Services.Cli.RemoteConfig.Model;
using Unity.Services.Cli.RemoteConfig.Service;
using Unity.Services.Cli.RemoteConfig.Types;
using Unity.Services.RemoteConfig.Editor.Authoring.Core.Model;
using Unity.Services.RemoteConfig.Editor.Authoring.Core.Networking;

namespace Unity.Services.Cli.RemoteConfig.UnitTest.Deploy;

[TestFixture]
class RemoteConfigClientTests
{
    const string k_TestProjectId = "a912b1fd-541d-42e1-89f2-85436f27aabd";
    const string k_TestEnvironmentId = "6d06a033-8a15-4919-8e8d-a731e08be87c";
    const string k_ConfigId = "97dfdc30-9bb8-46f2-89c5-7df39232e686";

    static readonly Array k_ValueTypeCases = Enum.GetValues(typeof(Types.ValueType));

    readonly Mock<IRemoteConfigService> m_MockRemoteConfigService = new ();
    readonly GetResponse m_ResponseWithConfig = new();

    RemoteConfigClient? m_RemoteConfigClient;

    [SetUp]
    public void SetUp()
    {
        m_MockRemoteConfigService.Reset();

        m_ResponseWithConfig.Configs = new List<Config>
        {
            new Config
            {
                CreatedAt = "2022-11-14T18:58:19Z",
                EnvironmentId = k_TestEnvironmentId,
                Id = k_ConfigId,
                ProjectId = k_TestProjectId,
                Type = RemoteConfigClient.k_ConfigType,
                UpdatedAt = "2022-11-16T21:38:34Z",
                Value = new List<RemoteConfigEntry>
                {
                    new RemoteConfigEntry
                    {
                        key = "color",
                        type = "string",
                        value = "black"
                    },
                    new RemoteConfigEntry
                    {
                        key = "length",
                        type = "float",
                        value = 123123
                    }
                }
            }
        };

        m_RemoteConfigClient = new RemoteConfigClient(
            m_MockRemoteConfigService.Object,
            k_TestProjectId,
            k_TestEnvironmentId,
            CancellationToken.None);
    }

    [Test]
    public void InitializeChangeProperties()
    {
        m_RemoteConfigClient = new RemoteConfigClient(m_MockRemoteConfigService.Object);
        Assert.Multiple(() =>
        {
            Assert.That(m_RemoteConfigClient.ProjectId, Is.EqualTo(string.Empty));
            Assert.That(m_RemoteConfigClient.EnvironmentId, Is.EqualTo(string.Empty));
            Assert.That(m_RemoteConfigClient.CancellationToken, Is.EqualTo(CancellationToken.None));
        });
        CancellationToken cancellationToken = new(true);
        m_RemoteConfigClient!.Initialize(k_TestProjectId, k_TestEnvironmentId, cancellationToken);
        Assert.Multiple(() =>
        {
            Assert.That(m_RemoteConfigClient.ProjectId, Is.SameAs(k_TestProjectId));
            Assert.That(m_RemoteConfigClient.EnvironmentId, Is.SameAs(k_TestEnvironmentId));
            Assert.That(m_RemoteConfigClient.CancellationToken, Is.EqualTo(cancellationToken));
        });
    }

    [TestCaseSource(nameof(k_ValueTypeCases))]
    public void GetValueFromStringReturnExpectedValue(Types.ValueType type)
    {
        Assert.That(RemoteConfigClient.GetValueFromString(type.ToString()), Is.EqualTo(type));
    }

    [Test]
    public void GetValueFromInvalidStringThrowCliException()
    {
        Assert.Throws<CliException>(() => RemoteConfigClient.GetValueFromString("foo"));
    }

    [Test]
    public async Task GetAsyncReturnConfigs()
    {
        var rawResponse = JsonConvert.SerializeObject(m_ResponseWithConfig);
        m_MockRemoteConfigService.Setup(r => r.GetAllConfigsFromEnvironmentAsync(k_TestProjectId, k_TestEnvironmentId,
            RemoteConfigClient.k_ConfigType, CancellationToken.None)).ReturnsAsync(rawResponse);

        var config = m_ResponseWithConfig.Configs?.FirstOrDefault(c => c.Type == RemoteConfigClient.k_ConfigType);
        var expectedResult = JsonConvert.SerializeObject(new GetConfigsResult(true, config!.Value));
        var result = JsonConvert.SerializeObject(await m_RemoteConfigClient!.GetAsync());
        StringAssert.AreEqualIgnoringCase(expectedResult, result);
    }

    [Test]
    public async Task GetAsyncReturnNoConfig()
    {
        GetResponse responseNoConfig = new();
        var rawResponse = JsonConvert.SerializeObject(responseNoConfig);
        m_MockRemoteConfigService.Setup(r => r.GetAllConfigsFromEnvironmentAsync(k_TestProjectId, k_TestEnvironmentId,
            RemoteConfigClient.k_ConfigType, CancellationToken.None)).ReturnsAsync(rawResponse);
        var expectedResult = new GetConfigsResult(false, null);
        var result = await m_RemoteConfigClient!.GetAsync();
        Assert.That(result, Is.EqualTo(expectedResult));
    }

    [Test]
    public async Task CreateAsyncSetConfigId()
    {
        var kvps = new List<ConfigValue>()
        {
            new ("color", Types.ValueType.String, "black"),
            new ("length", Types.ValueType.Float, 123123),
        };
        m_MockRemoteConfigService
            .Setup(rc =>
                rc.CreateConfigAsync(
                    k_TestProjectId,
                    k_TestEnvironmentId,
                    RemoteConfigClient.k_ConfigType,
                    kvps,
                    CancellationToken.None))
            .ReturnsAsync(k_ConfigId);
        await m_RemoteConfigClient!.CreateAsync(m_ResponseWithConfig.Configs!.First().Value!);
        Assert.That(m_RemoteConfigClient.ConfigId, Is.EqualTo(k_ConfigId));
    }

    [Test]
    public async Task UpdateAsyncSucceed()
    {
        var kvps = new List<ConfigValue>()
        {
            new ("color", Types.ValueType.String, "black"),
            new ("length", Types.ValueType.Float, 123123),
        };
        await m_RemoteConfigClient!.UpdateAsync(m_ResponseWithConfig.Configs!.First().Value!);
        m_MockRemoteConfigService.Verify(rc => rc.UpdateConfigAsync(k_TestProjectId, m_RemoteConfigClient.ConfigId, RemoteConfigClient.k_ConfigType,
            kvps, CancellationToken.None), Times.Once);
    }
}
