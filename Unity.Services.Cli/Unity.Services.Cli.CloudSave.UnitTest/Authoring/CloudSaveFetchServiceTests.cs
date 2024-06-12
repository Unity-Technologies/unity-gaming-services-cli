using Moq;
using Newtonsoft.Json;
using NUnit.Framework;
using Unity.Services.Cli.Authoring.Input;
using Unity.Services.Cli.CloudSave.Deploy;
using Unity.Services.Cli.CloudSave.Fetch;
using Unity.Services.DeploymentApi.Editor;
using Unity.Services.CloudSave.Authoring.Core.Deploy;
using Unity.Services.CloudSave.Authoring.Core.Fetch;
using Unity.Services.CloudSave.Authoring.Core.IO;
using Unity.Services.CloudSave.Authoring.Core.Model;
using Unity.Services.CloudSave.Authoring.Core.Service;

namespace Unity.Services.Cli.CloudSave.UnitTest.Authoring;

[TestFixture]
public class CloudSaveFetchServiceTests
{
    readonly Mock<ICloudSaveClient> m_MockClient = new();
    readonly Mock<ICloudSaveFetchHandler> m_MockFetchHandler = new();
    readonly Mock<ICloudSaveSimpleResourceLoader> m_MockLoader = new();
    CloudSaveFetchService? m_DeploymentService;
    Dictionary<string, SimpleResourceDeploymentItem>? m_Configs;

    [SetUp]
    public void SetUp()
    {
        m_MockClient.Reset();
        m_DeploymentService = new CloudSaveFetchService(
            m_MockFetchHandler.Object,
            m_MockClient.Object,
            m_MockLoader.Object);

        var config1 = new SimpleResourceDeploymentItem($"first_conf{Constants.SimpleFileExtension}");
        var config2 = new SimpleResourceDeploymentItem($"second_conf{Constants.SimpleFileExtension}");
        m_Configs = new[]
            {
                config1,
                config2
            }
            .ToDictionary(c => c.Path, c => c);

        m_MockLoader
            .Setup(
                m =>
                    m.ReadResource(
                        It.IsAny<string>(),
                        It.IsAny<CancellationToken>())
            )
            .Returns(
                (string s, CancellationToken c) =>
                {
                    if (m_Configs.TryGetValue(s, out var res))
                    {
                        return Task.FromResult((IResourceDeploymentItem)res);
                    }

                    throw new IOException($"'{s}' not found");
                });


        m_MockFetchHandler.Setup(
                d => d.FetchAsync(
                    It.IsAny<string>(),
                    It.IsAny<IReadOnlyList<IResourceDeploymentItem>>(),
                    It.IsAny<bool>(),
                    It.IsAny<bool>(),
                    It.IsAny<CancellationToken>()
                ))
            .ReturnsAsync(
                () =>
                {
                    config2.SetStatusSeverity(SeverityLevel.Success);
                    config1.SetStatusSeverity(SeverityLevel.Success);
                    config1.SetStatusDetail(Constants.Updated);
                    config2.SetStatusDetail(Constants.Deleted);
                    return new FetchResult()
                    {
                        Fetched = new[]
                        {
                            config1,
                            config2
                        }
                    };
                });
    }

    [Test]
    public async Task FetchAsync_MapsResult()
    {
        var input = new FetchInput()
        {
            Path = "rootdir",
            CloudProjectId = string.Empty
        };

        var res = await m_DeploymentService!.FetchAsync(
            input,
            new[]
            {
                $"first_conf{Constants.SimpleFileExtension}",
                $"second_conf{Constants.SimpleFileExtension}"
            },
            String.Empty,
            string.Empty,
            null,
            CancellationToken.None);

        Assert.Multiple(
            () =>
            {
                Assert.That(res.Created, Has.Count.EqualTo(0));
                Assert.That(res.Updated, Has.Count.EqualTo(1));
                Assert.That(res.Deleted, Has.Count.EqualTo(1));
                Assert.That(res.Fetched, Has.Count.EqualTo(2));
                Assert.That(res.Failed, Has.Count.EqualTo(0));
            });
    }

    [Test]
    public async Task FetchAsync_MapsFailed()
    {
        m_MockLoader
            .Setup(
                m =>
                    m.ReadResource(
                        $"fail_path{Constants.SimpleFileExtension}",
                        It.IsAny<CancellationToken>())
            )
            .ReturnsAsync(
                () => new SimpleResourceDeploymentItem($"fail_path{Constants.SimpleFileExtension}")
                {
                    Status = new DeploymentStatus("Failed to read", "...", SeverityLevel.Error)
                });

        var input = new FetchInput()
        {
            CloudProjectId = string.Empty
        };

        var res = await m_DeploymentService!.FetchAsync(
            input,
            new[]
            {
                $"first_conf{Constants.SimpleFileExtension}",
                $"second_conf{Constants.SimpleFileExtension}",
                $"fail_path{Constants.SimpleFileExtension}"
            },
            string.Empty,
            string.Empty,
            null,
            CancellationToken.None);

        Assert.Multiple(
            () =>
            {
                Assert.That(res.Created, Has.Count.EqualTo(0));
                Assert.That(res.Updated, Has.Count.EqualTo(1));
                Assert.That(res.Deleted, Has.Count.EqualTo(1));
                Assert.That(res.Fetched, Has.Count.EqualTo(2));
                Assert.That(res.Failed, Has.Count.EqualTo(1));
            });
    }
}
