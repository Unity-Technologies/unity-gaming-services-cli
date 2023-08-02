using NUnit.Framework;
using Unity.Services.Cli.Authoring.Model;
using Unity.Services.Cli.RemoteConfig.Deploy;
using Unity.Services.DeploymentApi.Editor;

namespace Unity.Services.Cli.RemoteConfig.UnitTest.Deploy;

public class RemoteConfigDeploymentResultTests
{
    RemoteConfigDeploymentResult m_RemoteConfigDeploymentResult;

    readonly IReadOnlyList<IDeploymentItem> m_AuthoredItems = new[]
    {
        new DeployContent("authored", "authored", "authored")
    };
    readonly IReadOnlyList<IDeploymentItem> m_UpdatedItems = new[]
    {
        new DeployContent("updated", "updated", "updated")
    };
    readonly IReadOnlyList<IDeploymentItem> m_CreatedItems = new[]
    {
        new DeployContent("created", "created", "created")
    };
    readonly IReadOnlyList<IDeploymentItem> m_DeletedItems = new[]
    {
        new DeployContent("deleted", "deleted", "deleted")
    };
    readonly IReadOnlyList<IDeploymentItem> m_FailedItems = new[]
    {
        new DeployContent("failed", "failed", "failed")
    };

    [SetUp]
    public void SetUp()
    {
        m_RemoteConfigDeploymentResult = new RemoteConfigDeploymentResult(
            m_UpdatedItems, m_DeletedItems, m_CreatedItems, m_AuthoredItems, m_FailedItems);
    }

    [Test]
    public void ToTableHasCorrectAmountRows()
    {
        var tableResult = m_RemoteConfigDeploymentResult.ToTable();

        var itemsCount = m_AuthoredItems.Count + m_UpdatedItems.Count + m_CreatedItems.Count + m_DeletedItems.Count + m_FailedItems.Count;

        Assert.That(tableResult.Result, Has.Count.EqualTo(itemsCount));
    }
}
