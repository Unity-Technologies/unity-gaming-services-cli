using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using Unity.Services.Cli.Authoring.Input;
using Unity.Services.Cli.Authoring.Model;
using Unity.Services.Cli.Economy.Authoring;
using Unity.Services.Cli.Economy.Authoring.Deploy;
using Unity.Services.Economy.Editor.Authoring.Core.Deploy;
using Unity.Services.Economy.Editor.Authoring.Core.Model;
using Unity.Services.Gateway.EconomyApiV2.Generated.Client;
using Statuses = Unity.Services.Cli.Authoring.Model.Statuses;

namespace Unity.Services.Cli.Economy.UnitTest.Authoring.Deploy;

public class EconomyDeploymentServiceTests
{

    const string k_ValidProjectId = "00000000-0000-0000-0000-000000000000";
    const string k_ValidEnvironmentId = "00000000-0000-0000-0000-000000000000";

    static readonly List<string> k_ValidFilePaths = new()
    {
        "test_1.ec",
        "test_2.ec",
        "test_3.ec",
        "test_4.ec"
    };

    DeployInput m_DefaultInput = new()
    {
        CloudProjectId = k_ValidProjectId,
        Reconcile = false
    };

    readonly Mock<ICliEconomyClient> m_MockCliEconomyClient = new();
    readonly Mock<IEconomyResourcesLoader> m_MockEconomyResourcesLoader = new();
    readonly Mock<IEconomyDeploymentHandler> m_MockEconomyDeploymentHandler = new();
    readonly Mock<ILogger> m_MockLogger = new();
    EconomyDeploymentService m_EconomyDeploymentService;

    public EconomyDeploymentServiceTests()
    {
        m_EconomyDeploymentService = new(
            m_MockCliEconomyClient.Object,
            m_MockEconomyResourcesLoader.Object,
            m_MockEconomyDeploymentHandler.Object
            );
    }

    static EconomyCurrency s_CreatedEconResource = new EconomyCurrency("TEST_ID_1")
    { Name = "TEST_ID_1" };
    static EconomyCurrency s_UpdatedEconResource = new EconomyCurrency("TEST_ID_2")
    { Name = "TEST_ID_2" };
    static EconomyCurrency s_DeletedEconResource = new EconomyCurrency("TEST_ID_3")
    { Name = "TEST_ID_3" };
    static EconomyCurrency s_FailedToReadEconResource = new EconomyCurrency("TEST_ID_4")
    { Name = "TEST_ID_4" };


    static List<DeployContent> s_DeployContents = new()
    {
        new(s_CreatedEconResource.Id, "", s_CreatedEconResource.Path, 0,
            Statuses.Loaded),
        new(s_UpdatedEconResource.Id, "", s_UpdatedEconResource.Path, 0,
            Statuses.Loaded),
        new(s_DeletedEconResource.Id, "", s_DeletedEconResource.Path, 0,
            Statuses.Loaded),
        new(s_FailedToReadEconResource.Id, "", s_FailedToReadEconResource.Path, 0,
            Statuses.FailedToRead),
    };

    static List<IEconomyResource> s_ResourcesList = new List<IEconomyResource>()
    {
        s_CreatedEconResource,
        s_UpdatedEconResource,
        s_DeletedEconResource,
        s_FailedToReadEconResource
    };


    static DeployResult s_DeployResult = new()
    {
        Created = new List<IEconomyResource>() { s_CreatedEconResource },
        Updated = new List<IEconomyResource>() { s_UpdatedEconResource },
        Deleted = new List<IEconomyResource>() { s_DeletedEconResource },
        Deployed = new List<IEconomyResource>() { s_CreatedEconResource, s_UpdatedEconResource },
        Failed = new List<IEconomyResource>() { s_FailedToReadEconResource }
    };

    [SetUp]
    public void SetUp()
    {
        m_MockCliEconomyClient.Reset();
        m_MockEconomyResourcesLoader.Reset();
        m_MockEconomyDeploymentHandler.Reset();
        m_MockLogger.Reset();

        for (int i = 0; i < k_ValidFilePaths.Count; i++)
        {
            m_MockEconomyResourcesLoader.Setup(d =>
                    d.LoadResourceAsync(k_ValidFilePaths[i], It.IsAny<CancellationToken>()))
                .ReturnsAsync(s_ResourcesList[i]);
        }


        m_MockEconomyDeploymentHandler.Setup(x =>
                x.DeployAsync(
                    It.IsAny<List<IEconomyResource>>(),
                    It.IsAny<bool>(),
                    It.IsAny<bool>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(s_DeployResult);
    }

    [Test]
    public async Task DeployAsync_CallsInitializeCorrectly()
    {
        await m_EconomyDeploymentService.Deploy(
            m_DefaultInput,
            k_ValidFilePaths,
            k_ValidProjectId,
            k_ValidEnvironmentId,
            null!,
            CancellationToken.None);

        m_MockCliEconomyClient.Verify(
            x => x.Initialize(
                k_ValidProjectId,
                k_ValidEnvironmentId,
                CancellationToken.None),
            Times.Once);
    }

    [Test]
    public async Task DeployAsync_CallsLoadScriptsAsyncCorrectly()
    {
        await m_EconomyDeploymentService.Deploy(
            m_DefaultInput,
            k_ValidFilePaths,
            k_ValidProjectId,
            k_ValidEnvironmentId,
            null!,
            CancellationToken.None);

        m_MockEconomyResourcesLoader.Verify(x =>
                x.LoadResourceAsync(
                    k_ValidFilePaths[0],
                    CancellationToken.None),
            Times.Once);
    }

    [Test]
    public async Task DeployAsync_CallsDeployAsyncCorrectly()
    {
        await m_EconomyDeploymentService.Deploy(
            m_DefaultInput,
            k_ValidFilePaths,
            k_ValidProjectId,
            k_ValidEnvironmentId,
            null!,
            CancellationToken.None);

        m_MockEconomyDeploymentHandler.Verify(x =>
                x.DeployAsync(
                    s_ResourcesList,
                    m_DefaultInput.DryRun,
                    m_DefaultInput.Reconcile,
                    CancellationToken.None),
            Times.Once);
    }

    [Test]
    public void DeployAsync_DoesNotThrowOnApiException()
    {
        m_MockEconomyDeploymentHandler.Setup(ex => ex
                .DeployAsync(
                    It.IsAny<List<IEconomyResource>>(),
                    false,
                    false,
                    It.IsAny<CancellationToken>())
            )
            .ThrowsAsync(new ApiException());

        Assert.DoesNotThrowAsync(() =>
            m_EconomyDeploymentService.Deploy(
                m_DefaultInput,
                k_ValidFilePaths,
                k_ValidProjectId,
                k_ValidEnvironmentId,
                null!,
                CancellationToken.None)
        );
    }

    [Test]
    public async Task DeployAsync_ReturnsCorrectResultForUpdatedCreatedAndDeleted()
    {
        var deploymentResult = await m_EconomyDeploymentService.Deploy(
            m_DefaultInput,
            k_ValidFilePaths,
            k_ValidProjectId,
            k_ValidEnvironmentId,
            null!,
            CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(deploymentResult.Created.First().Name, Is.EqualTo(s_DeployResult.Created.First().Id));
            Assert.That(deploymentResult.Updated.First().Name, Is.EqualTo(s_DeployResult.Updated.First().Id));
            Assert.That(deploymentResult.Deleted.First().Name, Is.EqualTo(s_DeployResult.Deleted.First().Id));
        });
    }

    [Test]
    public async Task DeployAsync_ReturnsCorrectResultForDeployed()
    {
        var deploymentResult = await m_EconomyDeploymentService.Deploy(
            m_DefaultInput,
            k_ValidFilePaths,
            k_ValidProjectId,
            k_ValidEnvironmentId,
            null!,
            CancellationToken.None);

        Assert.That(deploymentResult.Deployed.Count, Is.EqualTo(s_DeployResult.Deployed.Count));
        foreach (var deployContent in deploymentResult.Deployed)
        {
            Assert.NotNull(
                s_DeployResult.Deployed.Find(x =>
                    string.Equals(x.Id, deployContent.Name)));
        }
    }

    [Test]
    public async Task DeployAsync_ReturnsCorrectResultForFailed()
    {
        var deploymentResult = await m_EconomyDeploymentService.Deploy(
            m_DefaultInput,
            k_ValidFilePaths,
            k_ValidProjectId,
            k_ValidEnvironmentId,
            null!,
            CancellationToken.None);

        var failedContent = s_DeployContents.ToList()
            .FindAll(x => x.Status.Message == Statuses.FailedToRead);

        Assert.That(deploymentResult.Failed.Count, Is.EqualTo(failedContent.Count));
        foreach (var deployContent in deploymentResult.Failed)
        {
            Assert.NotNull(
                failedContent.Find(x =>
                    x.Name == deployContent.Name));
        }
    }
}
