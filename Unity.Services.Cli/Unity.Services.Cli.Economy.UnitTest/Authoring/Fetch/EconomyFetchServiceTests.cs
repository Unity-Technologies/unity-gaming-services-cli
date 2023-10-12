using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using Unity.Services.Cli.Authoring.Input;
using Unity.Services.Cli.Common.Utils;
using Unity.Services.Cli.Economy.Authoring;
using Unity.Services.Cli.Economy.Authoring.Fetch;
using Unity.Services.DeploymentApi.Editor;
using Unity.Services.Economy.Editor.Authoring.Core.Fetch;
using Unity.Services.Economy.Editor.Authoring.Core.Model;
using FetchResult = Unity.Services.Cli.Authoring.Model.FetchResult;

namespace Unity.Services.Cli.Economy.UnitTest.Authoring.Fetch;

[TestFixture]
class EconomyFetchServiceTests
{
    const string k_ValidProjectId = "12345678-1111-2222-3333-123412341234";

    readonly FetchInput m_FetchInput = new()
    {
        TargetEnvironmentName = "test",
        CloudProjectId = k_ValidProjectId,
        Path = "."
    };

    static readonly Mock<IUnityEnvironment> k_UnityEnvironment = new();
    static readonly Mock<ICliEconomyClient> k_EconomyClient = new();
    static readonly Mock<IEconomyResourcesLoader> k_EconomyResourcesLoader = new();
    static readonly Mock<IEconomyFetchHandler> k_EconomyFetchHandler = new();

    readonly EconomyFetchService m_EconomyService = new(
        k_UnityEnvironment.Object,
        k_EconomyClient.Object,
        k_EconomyResourcesLoader.Object,
        k_EconomyFetchHandler.Object);

    [SetUp]
    public void SetUp()
    {
        k_UnityEnvironment.Reset();
        k_EconomyClient.Reset();
        k_EconomyResourcesLoader.Reset();
        k_EconomyFetchHandler.Reset();
    }

    // TODO: Test no longer useful, re-eval
    [Test]
    public async Task FetchAsyncReturnFetchResult()
    {
        var filePaths = new List<string>()
        {
            "resource1.ec",
            "resource2.ec",
            "resource3.ec"
        };

        EconomyCurrency createdResource = new EconomyCurrency("RESOURCE_1")
        {
            Path = "resource1.ec"
        };

        EconomyCurrency updatedResource = new EconomyCurrency("RESOURCE_2")
        {
            Path = "resource2.ec"
        };

        EconomyCurrency deletedResource = new EconomyCurrency("RESOURCE_3")
        {
            Path = "resource3.ec"
        };

        List<IEconomyResource> economyResources = new()
        {
            createdResource,
            updatedResource,
            deletedResource
        };

        var handlerFetchResult = new Unity.Services.Economy.Editor.Authoring.Core.Fetch.FetchResult
        {
            Created = new List<IEconomyResource>
            {
                createdResource
            },
            Updated = new List<IEconomyResource>
            {
                updatedResource
            },
            Deleted = new List<IEconomyResource>
            {
                deletedResource
            },
            Fetched = new List<IEconomyResource>
            {
                updatedResource,
                deletedResource,
                createdResource,
            },
            Failed = new List<IEconomyResource>()
        };

        var expectedFetchResult = new FetchResult(
            handlerFetchResult.Updated,
            handlerFetchResult.Deleted,
            handlerFetchResult.Created,
            handlerFetchResult.Fetched,
            Array.Empty<IDeploymentItem>());

        for (int i = 0; i < filePaths.Count; i++)
        {
            k_EconomyResourcesLoader.Setup(e =>
                    e.LoadResourceAsync(filePaths[i], CancellationToken.None))
                .ReturnsAsync(economyResources[i]);
        }

        k_UnityEnvironment.Setup(u => u.FetchIdentifierAsync(CancellationToken.None)).ReturnsAsync(k_ValidProjectId);

        k_EconomyFetchHandler.Setup(e =>
            e.FetchAsync(
                m_FetchInput.Path,
                economyResources,
                m_FetchInput.DryRun,
                m_FetchInput.Reconcile,
                CancellationToken.None)).ReturnsAsync(handlerFetchResult);

        var fetchResult = await m_EconomyService.FetchAsync(
            m_FetchInput,
            filePaths,
            string.Empty,
            string.Empty,
            null,
            CancellationToken.None);
        var comparer = new DeploymentItemComparer();
        CollectionAssert.AreEqual(expectedFetchResult.Created, fetchResult.Created, comparer, "Created collections are not equal");
        CollectionAssert.AreEqual(expectedFetchResult.Updated, fetchResult.Updated, comparer, "Updated collections are not equal");
        CollectionAssert.AreEqual(expectedFetchResult.Fetched, fetchResult.Fetched, comparer, "Fetched collections are not equal");
        CollectionAssert.AreEqual(expectedFetchResult.Deleted, fetchResult.Deleted, comparer, "Deleted collections are not equal");
        CollectionAssert.AreEqual(expectedFetchResult.Failed, fetchResult.Failed, comparer, "Failed collections are not equal");
    }

    class DeploymentItemComparer : IComparer
    {
        public int Compare(object? x, object? y)
        {
            if (x is IDeploymentItem xx && y is IDeploymentItem yy)
                return CompareInternal(xx, yy);
            return -1;
        }

        static int CompareInternal(
            IDeploymentItem x,
            IDeploymentItem y)
        {
            if (ReferenceEquals(x, y)) return 0;
            if (ReferenceEquals(null, y)) return 1;
            if (ReferenceEquals(null, x)) return -1;
            var nameComparison = string.Compare(x.Name, y.Name, StringComparison.Ordinal);
            if (nameComparison != 0)
                return nameComparison;
            var pathComparison = string.Compare(x.Path, y.Path, StringComparison.Ordinal);
            if (pathComparison != 0)
                return pathComparison;
            var progressComparison = x.Progress.CompareTo(y.Progress);
            return progressComparison;
        }
    }
}
