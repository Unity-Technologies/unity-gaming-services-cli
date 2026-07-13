using NUnit.Framework;
using Unity.Services.Cli.Authoring.Model;
using UnityEditor.Purchasing.Editor.Authoring.Core.Model;
using Unity.Services.Cli.Purchasing.Authoring;

namespace Unity.Services.Cli.Purchasing.UnitTest.Core;

[TestFixture]
class PurchasingDeploymentServiceMultiFileTests : PurchasingDeployFetchTestBase
{
    PurchasingDeploymentService? m_Service;

    [SetUp]
    public override void SetUp()
    {
        base.SetUp();
        m_FakeUcatCatalogLoader.ReadCatalogImpl =
            (path, _) => Task.FromResult(MakeEntry(path));
        m_Service = new PurchasingDeploymentService(
            m_FakeDeploymentHandler,
            m_FakeClient,
            m_FakeUcatCatalogLoader,
            m_FakeCsvCatalogLoader);
    }

    [Test]
    public async Task Deploy_MultipleUcatFiles_CallsDeployHandlerWithAllEntries()
    {
        var files = new[]
        {
            new AuthoringFile("a.ucat"),
            new AuthoringFile("b.ucat"),
            new AuthoringFile("c.ucat"),
        };

        await m_Service!.Deploy(
            MakeDeployInput(),
            files,
            "proj",
            "env",
            null,
            CancellationToken.None);

        Assert.That(m_FakeDeploymentHandler.LastEntries?.Count, Is.EqualTo(3));
    }

    [Test]
    public async Task Deploy_MixedSuccessAndFailed_BothAppearInResult()
    {
        m_FakeUcatCatalogLoader.ReadCatalogImpl = (path, _) =>
        {
            var asError = path.EndsWith("bad.ucat");
            return Task.FromResult(MakeEntry(path, asError));
        };

        var files = new[]
        {
            new AuthoringFile("good1.ucat"),
            new AuthoringFile("good2.ucat"),
            new AuthoringFile("bad.ucat"),
        };

        var result = await m_Service!.Deploy(
            MakeDeployInput(),
            files,
            "proj",
            "env",
            null,
            CancellationToken.None);

        Assert.That(result.Failed.Count, Is.EqualTo(1));
    }
}
