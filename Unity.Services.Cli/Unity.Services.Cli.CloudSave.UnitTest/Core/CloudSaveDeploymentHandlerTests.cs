using Moq;
using NUnit.Framework;
using Unity.Services.DeploymentApi.Editor;
using Unity.Services.CloudSave.Authoring.Core.Deploy;
using Unity.Services.CloudSave.Authoring.Core.IO;
using Unity.Services.CloudSave.Authoring.Core.Model;
using Unity.Services.CloudSave.Authoring.Core.Service;

namespace Unity.Services.Cli.CloudSave.UnitTest.Core;

[TestFixture]
public class CloudSaveDeploymentHandlerTests : CloudSaveDeployFetchTestBase
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
    public async Task DeployAsync_CreateCallsMade()
    {
        // 1, 2 ,3
        var localResources = GetLocalResources();
        // 3, 4, 5
        var remoteResources = GetRemoteResources();

        Mock<ICloudSaveClient> mockClient = new();
        var handler = new CloudSaveDeploymentHandler(mockClient.Object);

        mockClient
            .Setup(c => c.List(It.IsAny<CancellationToken>()))
            .ReturnsAsync(remoteResources.ToList());

        var actualRes = await handler.DeployAsync(
            localResources
        );

        mockClient
            .Verify(
                c => c.Create(
                    It.Is<IResource>(l => l.Id == "ID1"),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        mockClient
            .Verify(
                c => c.Create(
                    It.Is<IResource>(l => l.Id == "ID2"),
                    It.IsAny<CancellationToken>()),
                Times.Once);
    }

    [Test]
    public async Task DeployAsync_UpdateCallsMade()
    {
        // 1, 2 ,3
        var localResources = GetLocalResources();
        // 3, 4, 5
        var remoteResources = GetRemoteResources();

        Mock<ICloudSaveClient> mockClient = new();
        var handler = new CloudSaveDeploymentHandler(mockClient.Object);

        mockClient
            .Setup(c => c.List(It.IsAny<CancellationToken>()))
            .ReturnsAsync(remoteResources.ToList());

        var actualRes = await handler.DeployAsync(
            localResources
        );

        mockClient
            .Verify(
                c => c.Update(
                    It.Is<IResource>(l => l.Id == "ID3"),
                    It.IsAny<CancellationToken>()),
                Times.Once);
    }


    [Test]
    public async Task DeployAsync_NoReconcileNoDeleteCalls()
    {
        // 1, 2 ,3
        var localResources = GetLocalResources();
        // 3, 4, 5
        var remoteResources = GetRemoteResources();

        Mock<ICloudSaveClient> mockClient = new();
        var handler = new CloudSaveDeploymentHandler(mockClient.Object);

        mockClient
            .Setup(c => c.List(It.IsAny<CancellationToken>()))
            .ReturnsAsync(remoteResources.ToList());

        var actualRes = await handler.DeployAsync(
            localResources
        );

        mockClient
            .Verify(
                c => c.Delete(
                    It.IsAny<IResource>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
    }

    [Test]
    public async Task DeployAsync_ReconcileDeleteCalls()
    {
        // 1, 2 ,3
        var localResources = GetLocalResources();
        // 3, 4, 5
        var remoteResources = GetRemoteResources();

        Mock<ICloudSaveClient> mockClient = new();
        var handler = new CloudSaveDeploymentHandler(mockClient.Object);

        mockClient
            .Setup(c => c.List(It.IsAny<CancellationToken>()))
            .ReturnsAsync(remoteResources.ToList());

        var actualRes = await handler.DeployAsync(
            localResources,
            reconcile: true
        );

        mockClient
            .Verify(
                c => c.Delete(
                    It.Is<IResource>(l => l.Id == "ID4"),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        mockClient
            .Verify(
                c => c.Delete(
                    It.Is<IResource>(l => l.Id == "ID5"),
                    It.IsAny<CancellationToken>()),
                Times.Once);
    }

    [Test]
    [TestCase(true)]
    [TestCase(false)]
    public async Task DeployAsync_DryRunCorrectResult(bool reconcile)
    {
        var localResources = GetLocalResources();
        var remoteResources = GetRemoteResources();

        Mock<ICloudSaveClient> mockClient = new();
        var handler = new CloudSaveDeploymentHandler(mockClient.Object);

        mockClient
            .Setup(c => c.List(It.IsAny<CancellationToken>()))
            .ReturnsAsync(remoteResources.ToList());

        var actualRes = await handler.DeployAsync(
            localResources,
            dryRun: true,
            reconcile: reconcile
        );

        Assert.That(
            actualRes.Deployed.ToList(), Does.Contain(actualRes.Deployed.FirstOrDefault(
                l => l.Resource.Id == "ID1" && l.Status.MessageDetail.StartsWith(Constants.Created))),
            "Item marked for creation is missing or incorrectly labeled");
        Assert.That(
            actualRes.Deployed.ToList(), Does.Contain(actualRes.Deployed.FirstOrDefault(
                l => l.Resource.Id == "ID2" && l.Status.MessageDetail.StartsWith(Constants.Created))),
            "Item marked for creation is missing or incorrectly labeled");
        Assert.That(
            actualRes.Deployed.ToList(), Does.Contain(actualRes.Deployed.FirstOrDefault(
                l => l.Resource.Id == "ID3" && l.Status.MessageDetail.StartsWith(Constants.Updated))),
            "Item marked for update is missing or incorrectly labeled");
        if (reconcile)
        {
            Assert.Multiple(() =>
            {
                Assert.That(
                    actualRes.Deployed.ToList(), Does.Contain(actualRes.Deployed.FirstOrDefault(
                        l => l.Resource.Id == "ID4" && l.Status.MessageDetail.StartsWith(Constants.Deleted))),
                    "Item marked for deletion is missing or incorrectly labeled");
                Assert.That(
                            actualRes.Deployed.ToList(), Does.Contain(actualRes.Deployed.FirstOrDefault(
                                l => l.Resource.Id == "ID5" && l.Status.MessageDetail.StartsWith(Constants.Deleted))),
                            "Item marked for deletion is missing or incorrectly labeled");
                Assert.That(actualRes.Deployed, Has.Count.EqualTo(5));
            });
        }
        else
        {
            Assert.That(actualRes.Deployed, Has.Count.EqualTo(3));
        }
    }


    [Test]
    [TestCase(true)]
    [TestCase(false)]
    public async Task DeployAsync_DryRunNoMutatingCalls(bool reconcile)
    {
        var localResources = GetLocalResources();
        var remoteResources = GetRemoteResources();

        Mock<ICloudSaveClient> mockClient = new();
        var handler = new CloudSaveDeploymentHandler(mockClient.Object);

        mockClient
            .Setup(c => c.List(It.IsAny<CancellationToken>()))
            .ReturnsAsync(remoteResources.ToList());

        var actualRes = await handler.DeployAsync(
            localResources,
            dryRun: true,
            reconcile: reconcile
        );

        mockClient
            .Verify(
                l => l.Create(
                    It.IsAny<IResource>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
        mockClient
            .Verify(
                l => l.Update(
                    It.IsAny<IResource>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
        mockClient
            .Verify(
                l => l.Delete(
                    It.IsAny<IResource>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
    }

    [Test]
    [TestCase(false, false)]
    [TestCase(false, true)]
    [TestCase(true, false)]
    [TestCase(true, true)]
    public async Task DeployAsync_DuplicateIdNotDeleted(bool dryRun, bool reconcile)
    {
        // 1, 2, 3
        var localResources = GetLocalResources();
        localResources.Add(
            new SimpleResourceDeploymentItem("sub2/one.serv")
            {
                Resource = new SimpleResource()
                {
                    Id = "ID1"
                }
            }
        );
        //3, 4, 5
        var remoteResources = GetRemoteResources();

        Mock<ICloudSaveClient> mockClient = new();
        var handler = new CloudSaveDeploymentHandler(mockClient.Object);

        mockClient
            .Setup(c => c.List(It.IsAny<CancellationToken>()))
            .ReturnsAsync(remoteResources.ToList());

        var actualRes = await handler.DeployAsync(
            localResources,
            dryRun: dryRun,
            reconcile: reconcile
        );

        var failed = actualRes
            .Deployed
            .Where(i => i.Status.MessageSeverity == SeverityLevel.Error)
            .ToList();
        Assert.Multiple(() =>
        {
            Assert.That(failed.Count(l => l.Resource.Id == "ID1"), Is.EqualTo(2));
            Assert.That(failed.FirstOrDefault(l => l.Path == "sub2/one.serv"), Is.Not.Null);
            Assert.That(failed.FirstOrDefault(l => l.Path == "one.serv"), Is.Not.Null);
        });
        mockClient.Verify(c => c.Delete(failed[0].Resource, It.IsAny<CancellationToken>()), Times.Never());
        mockClient.Verify(c => c.Delete(failed[1].Resource, It.IsAny<CancellationToken>()), Times.Never());
    }

    [Test]
    [TestCase(false, false)]
    [TestCase(false, true)]
    [TestCase(true, false)]
    [TestCase(true, true)]
    public async Task DeployAsync_CorrectResults(bool dryRun, bool reconcile)
    {
        // 1, 2, 3
        var localResources = GetLocalResources();
        // 3, 4, 5
        var remoteResources = GetRemoteResources();

        Mock<ICloudSaveClient> mockClient = new();
        Mock<ICloudSaveSimpleResourceLoader> mockLoader = new();
        var handler = new CloudSaveDeploymentHandler(mockClient.Object);

        mockClient
            .Setup(c => c.List(It.IsAny<CancellationToken>()))
            .ReturnsAsync(remoteResources.ToList());

        var actualRes = await handler.DeployAsync(
            localResources,
            dryRun: true,
            reconcile: reconcile
        );


        Assert.That(actualRes.Deployed, Has.Count.EqualTo(reconcile ? 5 : 3));

        foreach (var localConfig in localResources)
        {
            Assert.That(actualRes.Deployed.ToList(), Does.Contain(localConfig));
        }

        StringAssert.StartsWith(Constants.Created, actualRes.Deployed[0].Status.MessageDetail);
        StringAssert.StartsWith(Constants.Created, actualRes.Deployed[1].Status.MessageDetail);
        StringAssert.StartsWith(Constants.Updated, actualRes.Deployed[2].Status.MessageDetail);

        if (reconcile)
        {
            StringAssert.StartsWith(Constants.Deleted, actualRes.Deployed[3].Status.MessageDetail);
            StringAssert.StartsWith(Constants.Deleted, actualRes.Deployed[4].Status.MessageDetail);
        }

        if (!dryRun)
        {
            foreach (var localConfig in actualRes.Deployed)
            {
                Assert.That(localConfig.Status.MessageSeverity, Is.EqualTo(SeverityLevel.Success));
            }
        }
    }
}
