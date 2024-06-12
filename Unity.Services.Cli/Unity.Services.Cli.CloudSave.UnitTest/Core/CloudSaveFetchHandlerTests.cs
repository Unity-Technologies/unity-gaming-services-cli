using Moq;
using NUnit.Framework;
using Unity.Services.DeploymentApi.Editor;
using Unity.Services.CloudSave.Authoring.Core.Deploy;
using Unity.Services.CloudSave.Authoring.Core.Fetch;
using Unity.Services.CloudSave.Authoring.Core.IO;
using Unity.Services.CloudSave.Authoring.Core.Model;
using Unity.Services.CloudSave.Authoring.Core.Service;

namespace Unity.Services.Cli.CloudSave.UnitTest.Core;

[TestFixture]
public class CloudSaveFetchHandlerTests : CloudSaveDeployFetchTestBase
{
    [Test]
    public async Task Test_NoDuplicates_NoProblem()
    {
        var mockClient = new Mock<ICloudSaveClient>();
        mockClient.Setup(c => c.List(It.IsAny<CancellationToken>()))
            .Returns(() => Task.FromResult((IReadOnlyList<IResource>)Array.Empty<IResource>()));
        mockClient.Setup(c => c.Create(It.IsAny<IResource>(), It.IsAny<CancellationToken>()))
            .Returns(() => Task.CompletedTask);
        mockClient.Setup(c => c.Update(It.IsAny<IResource>(), It.IsAny<CancellationToken>()))
            .Returns(() => Task.CompletedTask);
        mockClient.Setup(c => c.Get(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(
                () => Task.FromResult<IResource>(
                    new SimpleResource
                    {
                        Id = "one"
                    }));

        var moduleTemplateClient = mockClient.Object;
        var handler = new CloudSaveDeploymentHandler(moduleTemplateClient);
        var localResources = new[]
        {
            new SimpleResourceDeploymentItem("one.serv")
            {
                Resource = new SimpleResource
                {
                    Id = "one"
                }
            },
            new SimpleResourceDeploymentItem("two.serv")
            {
                Resource = new SimpleResource
                {
                    Id = "two"
                }
            }
        };
        var res = await handler.DeployAsync(localResources);
        Assert.Multiple(
            () =>
            {
                Assert.That(res.Deployed.Count(r => r.Status.MessageSeverity == SeverityLevel.Success), Is.EqualTo(2));
                Assert.That(res.Deployed.Count(r => r.Status.MessageSeverity == SeverityLevel.Error), Is.EqualTo(0));
            });
    }

    [Test]
    public async Task Test_TwoDuplicates_TwoFailed()
    {
        var mockClient = new Mock<ICloudSaveClient>();
        mockClient.Setup(c => c.List(It.IsAny<CancellationToken>()))
            .Returns(() => Task.FromResult((IReadOnlyList<IResource>)Array.Empty<IResource>()));
        mockClient.Setup(c => c.Create(It.IsAny<IResource>(), It.IsAny<CancellationToken>()))
            .Returns(() => Task.CompletedTask);
        mockClient.Setup(c => c.Update(It.IsAny<IResource>(), It.IsAny<CancellationToken>()))
            .Returns(() => Task.CompletedTask);
        mockClient.Setup(c => c.Get(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(
                () => Task.FromResult<IResource>(
                    new SimpleResource
                    {
                        Id = "one"
                    }));

        var moduleTemplateClient = mockClient.Object;
        var handler = new CloudSaveDeploymentHandler(moduleTemplateClient);
        var res = await handler.DeployAsync(
            new[]
            {
                new SimpleResourceDeploymentItem("one.serv")
                {
                    Resource = new SimpleResource
                    {
                        Id = "one"
                    }
                },
                new SimpleResourceDeploymentItem("sub1/one.serv")
                {
                    Resource = new SimpleResource
                    {
                        Id = "one"
                    }
                }
            });
        Assert.Multiple(
            () =>
            {
                Assert.That(res.Deployed.Count(r => r.Status.MessageSeverity == SeverityLevel.Success), Is.EqualTo(0));
                Assert.That(res.Deployed.Count(r => r.Status.MessageSeverity == SeverityLevel.Error), Is.EqualTo(2));
            });
    }

    [Test]
    public async Task Test_TwoDuplicates_OneDistinct()
    {
        var mockClient = new Mock<ICloudSaveClient>();
        mockClient.Setup(c => c.List(It.IsAny<CancellationToken>()))
            .Returns(() => Task.FromResult((IReadOnlyList<IResource>)Array.Empty<IResource>()));
        mockClient.Setup(c => c.Create(It.IsAny<IResource>(), It.IsAny<CancellationToken>()))
            .Returns(() => Task.CompletedTask);
        mockClient.Setup(c => c.Update(It.IsAny<IResource>(), It.IsAny<CancellationToken>()))
            .Returns(() => Task.CompletedTask);
        mockClient.Setup(c => c.Get(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(
                () => Task.FromResult<IResource>(
                    new SimpleResource
                    {
                        Id = "one"
                    }));

        var handler = new CloudSaveDeploymentHandler(mockClient.Object);
        var res = await handler.DeployAsync(
            new[]
            {
                new SimpleResourceDeploymentItem("one.serv")
                {
                    Resource = new SimpleResource()
                    {
                        Id = "one"
                    }
                },
                new SimpleResourceDeploymentItem("sub1/one.serv")
                {
                    Resource = new SimpleResource()
                    {
                        Id = "one"
                    }
                },
                new SimpleResourceDeploymentItem("sub2/two.serv")
                {
                    Resource = new SimpleResource()
                    {
                        Id = "two"
                    }
                }
            });

        Assert.Multiple(
            () =>
            {
                Assert.That(res.Deployed.Count(r => r.Status.MessageSeverity == SeverityLevel.Success), Is.EqualTo(1));
                Assert.That(res.Deployed.Count(r => r.Status.MessageSeverity == SeverityLevel.Error), Is.EqualTo(2));
            });
    }

    [Test]
    public async Task FetchAsync_WriteCallsMade()
    {
        // 1, 2, 3
        var localResources = GetLocalResources();
        // 3, 4, 5
        var remoteResources = GetRemoteResources();

        Mock<ICloudSaveClient> mockClient = new();
        Mock<ICloudSaveSimpleResourceLoader> mockLoader = new();
        var handler = new CloudSaveFetchHandler(mockClient.Object, mockLoader.Object);

        mockClient
            .Setup(c => c.List(It.IsAny<CancellationToken>()))
            .ReturnsAsync(remoteResources.ToList());

        var actualRes = await handler.FetchAsync(
            "dir",
            localResources
        );

        var thirdRes = localResources.First(c => c.Resource.Id == "ID3");

        mockLoader
            .Verify(
                f => f.CreateOrUpdateResource(
                    thirdRes,
                    It.IsAny<CancellationToken>()),
                Times.Once);

        mockLoader
            .Verify(
                f => f.CreateOrUpdateResource(
                    localResources.First(c => c.Resource.Id != "ID3"),
                    It.IsAny<CancellationToken>()),
                Times.Never,
                "Something other than the expected file was written into");
    }

    [Test]
    public async Task FetchAsync_DeleteCallsMade()
    {
        // 1, 2, 3
        var localResources = GetLocalResources();
        // 3, 4, 5
        var remoteResources = GetRemoteResources();

        Mock<ICloudSaveClient> mockClient = new();
        Mock<ICloudSaveSimpleResourceLoader> mockLoader = new();
        var handler = new CloudSaveFetchHandler(mockClient.Object, mockLoader.Object);

        mockClient
            .Setup(c => c.List(It.IsAny<CancellationToken>()))
            .ReturnsAsync(remoteResources.ToList());

        var actualRes = await handler.FetchAsync(
            "dir",
            localResources
        );

        var res1 = localResources.First(c => c.Resource.Id == "ID1");
        var res2 = localResources.First(c => c.Resource.Id == "ID2");

        mockLoader
            .Verify(
                f => f.DeleteResource(
                    res1,
                    It.IsAny<CancellationToken>()),
                Times.Once);

        mockLoader
            .Verify(
                f => f.DeleteResource(
                    res2,
                    It.IsAny<CancellationToken>()),
                Times.Once);
    }

    [Test]
    public async Task FetchAsync_WriteNewOnReconcile()
    {
        // 1, 2, 3
        var localResources = GetLocalResources();
        // 3, 4, 5
        var remoteResources = GetRemoteResources();

        Mock<ICloudSaveClient> mockClient = new();
        Mock<ICloudSaveSimpleResourceLoader> mockLoader = new();
        var handler = new CloudSaveFetchHandler(mockClient.Object, mockLoader.Object);

        mockClient
            .Setup(c => c.List(It.IsAny<CancellationToken>()))
            .ReturnsAsync(remoteResources.ToList());

        var actualRes = await handler.FetchAsync(
            "dir",
            localResources,
            reconcile: true
        );

        var writtenFileIds = new[]
        {
            "ID4",
            "ID5"
        };

        foreach (var fileId in writtenFileIds)
        {
            mockLoader
                .Verify(
                    f => f.CreateOrUpdateResource(
                        It.Is<IResourceDeploymentItem>(i => i.Resource.Id == fileId),
                        It.IsAny<CancellationToken>()),
                    Times.Once);
        }
    }

    [Test]
    [TestCase(true)]
    [TestCase(false)]
    public async Task FetchAsync_DryRunNoCalls(bool reconcile)
    {
        // 1, 2, 3
        var localResources = GetLocalResources();
        // 3, 4, 5
        var remoteResources = GetRemoteResources();

        Mock<ICloudSaveClient> mockClient = new();
        Mock<ICloudSaveSimpleResourceLoader> mockLoader = new();
        var handler = new CloudSaveFetchHandler(mockClient.Object, mockLoader.Object);

        mockClient
            .Setup(c => c.List(It.IsAny<CancellationToken>()))
            .ReturnsAsync(remoteResources.ToList());

        var actualRes = await handler.FetchAsync(
            "dir",
            localResources,
            dryRun: true,
            reconcile: reconcile
        );

        mockLoader
            .Verify(
                f => f.DeleteResource(
                    It.IsAny<IResourceDeploymentItem>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

        mockLoader
            .Verify(
                f => f.CreateOrUpdateResource(
                    It.IsAny<IResourceDeploymentItem>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
    }

    [Test]
    public async Task FetchAsync_DuplicateIdNotDeleted()
    {
        var localResources = GetLocalResources();
        var triggerConfig = new SimpleResourceDeploymentItem("other/my-thing.serv")
        {
            Resource = new SimpleResource
            {
                Id = "ID1"
            }
        };

        localResources.Add(triggerConfig);
        var remoteResources = GetRemoteResources();

        Mock<ICloudSaveClient> mockClient = new();
        Mock<ICloudSaveSimpleResourceLoader> mockLoader = new();
        var handler = new CloudSaveFetchHandler(mockClient.Object, mockLoader.Object);

        mockClient
            .Setup(c => c.List(It.IsAny<CancellationToken>()))
            .ReturnsAsync(remoteResources.ToList());

        var actualRes = await handler.FetchAsync(
            "dir",
            localResources,
            dryRun: true
        );

        var resources = localResources
            .Where(l => l.Resource.Id == "ID1")
            .ToList();

        foreach (var res in resources)
        {
            mockLoader
                .Verify(
                    f => f.DeleteResource(
                        res,
                        It.IsAny<CancellationToken>()),
                    Times.Never);

            mockLoader
                .Verify(
                    f => f.CreateOrUpdateResource(
                        res,
                        It.IsAny<CancellationToken>()),
                    Times.Never);
        }
    }

    [Test]
    [TestCase(false, false)]
    [TestCase(false, true)]
    [TestCase(true, false)]
    [TestCase(true, true)]
    public async Task FetchAsync_CorrectResults(bool dryRun, bool reconcile)
    {
        // 1, 2, 3
        var localResources = GetLocalResources();
        // 3, 4, 5
        var remoteResources = GetRemoteResources();

        Mock<ICloudSaveClient> mockClient = new();
        Mock<ICloudSaveSimpleResourceLoader> mockLoader = new();
        var handler = new CloudSaveFetchHandler(mockClient.Object, mockLoader.Object);

        mockClient
            .Setup(c => c.List(It.IsAny<CancellationToken>()))
            .ReturnsAsync(remoteResources.ToList());

        var actualRes = await handler.FetchAsync(
            "dir",
            localResources,
            dryRun: true,
            reconcile: reconcile
        );


        Assert.That(actualRes.Fetched, Has.Count.EqualTo(reconcile ? 5 : 3));

        foreach (var localConfig in localResources)
        {
            Assert.That(actualRes.Fetched.ToList(), Does.Contain(localConfig));
        }

        StringAssert.StartsWith(Constants.Deleted, actualRes.Fetched[0].Status.MessageDetail);
        StringAssert.StartsWith(Constants.Deleted, actualRes.Fetched[1].Status.MessageDetail);
        StringAssert.StartsWith(Constants.Updated, actualRes.Fetched[2].Status.MessageDetail);

        if (reconcile)
        {
            StringAssert.StartsWith(Constants.Created, actualRes.Fetched[3].Status.MessageDetail);
            StringAssert.StartsWith(Constants.Created, actualRes.Fetched[4].Status.MessageDetail);
        }

        if (!dryRun)
        {
            foreach (var localConfig in actualRes.Fetched)
            {
                Assert.That(localConfig.Status.MessageSeverity, Is.EqualTo(SeverityLevel.Success));
            }
        }
    }
}
