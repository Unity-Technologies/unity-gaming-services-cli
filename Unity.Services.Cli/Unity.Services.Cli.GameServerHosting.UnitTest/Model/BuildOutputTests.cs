using System.Text;
using Unity.Services.Cli.GameServerHosting.Model;
using Unity.Services.Gateway.GameServerHostingApiV1.Generated.Model;

namespace Unity.Services.Cli.GameServerHosting.UnitTest.Model;

[TestFixture]
class BuildOutputTests
{
    [SetUp]
    public void SetUp()
    {
        m_BuildListItem = new BuildListInner(
            buildID: 1,
            buildName: "Test Build 1",
            buildConfigurations: 1,
            syncStatus: BuildListInner.SyncStatusEnum.SYNCED,
            ccd: new CCDDetails(
                Guid.Parse(ValidBucketId),
                Guid.Parse(ValidReleaseId)
            ),
            osFamily: BuildListInner.OsFamilyEnum.LINUX,
            updated: DateTime.Now
        );
        m_BuildCreateResponse = new CreateBuild200Response(
            1,
            "Test Build 1",
            syncStatus: CreateBuild200Response.SyncStatusEnum.SYNCED,
            ccd: new CCDDetails(
                Guid.Parse(ValidBucketId),
                Guid.Parse(ValidReleaseId)
            ),
            osFamily: CreateBuild200Response.OsFamilyEnum.LINUX,
            updated: DateTime.Now
        );
    }

    BuildListInner? m_BuildListItem;
    CreateBuild200Response? m_BuildCreateResponse;

    [Test]
    public void ConstructBuildOutputWithValidBuild()
    {
        BuildOutput output = new(m_BuildListItem!);
        Assert.Multiple(() =>
        {
            Assert.That(output.BuildId, Is.EqualTo(m_BuildListItem!.BuildID));
            Assert.That(output.BuildName, Is.EqualTo(m_BuildListItem!.BuildName));
            Assert.That(output.BuildConfigurations, Is.EqualTo(m_BuildListItem!.BuildConfigurations));
            Assert.That(output.SyncStatus, Is.EqualTo(m_BuildListItem!.SyncStatus));
            Assert.That(output.Ccd?.BucketId, Is.EqualTo(m_BuildListItem!.Ccd.BucketID));
            Assert.That(output.Ccd?.ReleaseId, Is.EqualTo(m_BuildListItem!.Ccd.ReleaseID));
            Assert.That(output.Container, Is.EqualTo(m_BuildListItem!.Container));
            Assert.That(output.OsFamily, Is.EqualTo(m_BuildListItem!.OsFamily));
            Assert.That(output.Updated, Is.EqualTo(m_BuildListItem!.Updated));
        });
        output = new BuildOutput(m_BuildCreateResponse!);
        Assert.Multiple(() =>
        {
            Assert.That(output.BuildId, Is.EqualTo(m_BuildCreateResponse!.BuildID));
            Assert.That(output.BuildName, Is.EqualTo(m_BuildCreateResponse!.BuildName));
            Assert.That(output.BuildConfigurations, Is.Null);
            Assert.That(output.SyncStatus, Is.EqualTo((BuildListInner.SyncStatusEnum)m_BuildCreateResponse!.SyncStatus));
            Assert.That(output.Ccd?.BucketId, Is.EqualTo(m_BuildCreateResponse!.Ccd.BucketID));
            Assert.That(output.Ccd?.ReleaseId, Is.EqualTo(m_BuildCreateResponse!.Ccd.ReleaseID));
            Assert.That(output.Container, Is.EqualTo(m_BuildCreateResponse!.Container));
            Assert.That(output.OsFamily, Is.EqualTo((BuildListInner.OsFamilyEnum?)m_BuildCreateResponse!.OsFamily));
            Assert.That(output.Updated, Is.EqualTo(m_BuildCreateResponse!.Updated));
        });
    }

    [Test]
    public void BuildOutputToString()
    {
        var sb = new StringBuilder();
        BuildOutput output = new(m_BuildListItem!);
        sb.AppendLine("buildName: Test Build 1");
        sb.AppendLine("buildId: 1");
        sb.AppendLine("osFamily: LINUX");
        sb.AppendLine("updated: " + m_BuildListItem!.Updated);
        sb.AppendLine("buildConfigurations: 1");
        sb.AppendLine("syncStatus: SYNCED");
        sb.AppendLine("ccd:");
        sb.AppendLine("  bucketId: " + ValidBucketId);
        sb.AppendLine("  releaseId: " + ValidReleaseId);
        Assert.That(output.ToString(), Is.EqualTo(sb.ToString()));
        sb.Clear();
        output = new BuildOutput(m_BuildCreateResponse!);
        sb.AppendLine("buildName: Test Build 1");
        sb.AppendLine("buildId: 1");
        sb.AppendLine("osFamily: LINUX");
        sb.AppendLine("updated: " + m_BuildCreateResponse!.Updated);
        sb.AppendLine("syncStatus: SYNCED");
        sb.AppendLine("ccd:");
        sb.AppendLine("  bucketId: " + ValidBucketId);
        sb.AppendLine("  releaseId: " + ValidReleaseId);
        Assert.That(output.ToString(), Is.EqualTo(sb.ToString()));
    }
}
