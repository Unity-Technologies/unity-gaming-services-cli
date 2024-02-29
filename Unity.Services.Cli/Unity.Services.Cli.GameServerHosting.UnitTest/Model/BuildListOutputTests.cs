using System.Text;
using Unity.Services.Cli.GameServerHosting.Model;
using Unity.Services.Gateway.GameServerHostingApiV1.Generated.Model;

namespace Unity.Services.Cli.GameServerHosting.UnitTest.Model;

[TestFixture]
class BuildListOutputTests
{
    [SetUp]
    public void SetUp()
    {
        m_Builds = new List<BuildListInner>
        {
            new(
                buildID: 1,
                buildName: "Test Build 1",
                buildConfigurations: 1,
                buildVersionName: ValidBuildVersionName,
                syncStatus: BuildListInner.SyncStatusEnum.SYNCED,
                ccd: new CCDDetails(
                    Guid.Parse(ValidBucketId),
                    Guid.Parse(ValidReleaseId)
                ),
                osFamily: BuildListInner.OsFamilyEnum.LINUX,
                updated: DateTime.Now
            ),
            new(
                buildID: 2,
                buildName: "Test Build 2",
                buildConfigurations: 2,
                buildVersionName: ValidBuildVersionName,
                syncStatus: BuildListInner.SyncStatusEnum.PENDING,
                container: new ContainerImage("2"),
                osFamily: BuildListInner.OsFamilyEnum.LINUX,
                updated: DateTime.Now
            )
        };
    }

    List<BuildListInner>? m_Builds;

    [Test]
    public void ConstructBuildListOutputWithValidList()
    {
        BuildListOutput output = new(m_Builds!);
        Assert.That(output, Has.Count.EqualTo(m_Builds!.Count));
        for (var i = 0; i < output.Count; i++)
            Assert.Multiple(
                () =>
                {
                    Assert.That(output[i].BuildId, Is.EqualTo(m_Builds[i].BuildID));
                    Assert.That(output[i].BuildName, Is.EqualTo(m_Builds[i].BuildName));
                    Assert.That(output[i].BuildConfigurations, Is.EqualTo(m_Builds[i].BuildConfigurations));
                    Assert.That(output[i].SyncStatus, Is.EqualTo(m_Builds[i].SyncStatus));
                    // ReSharper disable ConditionalAccessQualifierIsNonNullableAccordingToAPIContract
                    Assert.That(output[i].Ccd?.BucketId, Is.EqualTo(m_Builds[i].Ccd?.BucketID));
                    Assert.That(output[i].Ccd?.ReleaseId, Is.EqualTo(m_Builds[i].Ccd?.ReleaseID));
                    // ReSharper restore ConditionalAccessQualifierIsNonNullableAccordingToAPIContract
                    Assert.That(output[i].Container, Is.EqualTo(m_Builds[i].Container));
                    Assert.That(output[i].OsFamily, Is.EqualTo(m_Builds[i].OsFamily));
                    Assert.That(output[i].Updated, Is.EqualTo(m_Builds[i].Updated));
                });
    }

    [Test]
    public void BuildListOutputToString()
    {
        BuildListOutput output = new(m_Builds!);
        var sb = new StringBuilder();
        foreach (var build in output)
        {
            sb.AppendLine($"- buildVersionName: {build.BuildVersionName}");
            sb.AppendLine($"  buildName: {build.BuildName}");
            sb.AppendLine($"  buildId: {build.BuildId}");
            sb.AppendLine($"  osFamily: {build.OsFamily}");
            sb.AppendLine($"  updated: {build.Updated}");
            sb.AppendLine($"  buildConfigurations: {build.BuildConfigurations}");
            sb.AppendLine($"  syncStatus: {build.SyncStatus}");

            if (build.Ccd != null)
            {
                sb.AppendLine("  ccd:");
                sb.AppendLine($"    bucketId: {build.Ccd?.BucketId}");
                sb.AppendLine($"    releaseId: {build.Ccd?.ReleaseId}");
            }

            // Need to ignore some warning here because the nullable API contract is not correct
            // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
            // ReSharper disable once InvertIf
            if (build.Container != default)
            {
                sb.AppendLine("  container:");
                sb.AppendLine($"    imageTag: {build.Container.ImageTag}");
            }
        }

        Assert.That(output.ToString(), Is.EqualTo(sb.ToString()));
    }
}
