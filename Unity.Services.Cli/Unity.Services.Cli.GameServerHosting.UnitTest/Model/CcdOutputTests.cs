using System.Text;
using Unity.Services.Cli.GameServerHosting.Model;
using Unity.Services.Gateway.GameServerHostingApiV1.Generated.Model;

namespace Unity.Services.Cli.GameServerHosting.UnitTest.Model;

[TestFixture]
class CcdOutputTests
{
    [SetUp]
    public void Setup()
    {
        m_CcdDetails = new CCDDetails(
            Guid.Parse(ValidBucketId),
            Guid.Parse(ValidReleaseId)
        );
    }

    CCDDetails? m_CcdDetails;

    [Test]
    public void ConstructCcdOutputWithValidCcdDetails()
    {
        CcdOutput output = new(m_CcdDetails);
        Assert.Multiple(() =>
        {
            Assert.That(output.BucketId, Is.EqualTo(m_CcdDetails?.BucketID));
            Assert.That(output.ReleaseId, Is.EqualTo(m_CcdDetails?.ReleaseID));
        });
    }


    [Test]
    public void CcdOutputToString()
    {
        var sb = new StringBuilder();
        CcdOutput output = new(m_CcdDetails);
        sb.AppendLine($"bucketId: {output.BucketId}");
        sb.AppendLine($"releaseId: {output.ReleaseId}");
        Assert.That(output.ToString(), Is.EqualTo(sb.ToString()));
    }
}
