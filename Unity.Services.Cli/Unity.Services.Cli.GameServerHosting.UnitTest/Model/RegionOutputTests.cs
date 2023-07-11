using System.Text;
using Unity.Services.Cli.GameServerHosting.Model;
using Unity.Services.Gateway.GameServerHostingApiV1.Generated.Model;

namespace Unity.Services.Cli.GameServerHosting.UnitTest.Model;

[TestFixture]
public class RegionOutputTests
{
    [SetUp]
    public void SetUp()
    {
        m_RegionItem = new FleetRegionsTemplateListItem(
            regionID: new Guid(ValidTemplateRegionId),
            name: ValidTemplateRegionName
        );
    }

    FleetRegionsTemplateListItem? m_RegionItem;

    [Test]
    public void ConstructFleetListItemOutputWithValidList()
    {
        RegionTemplateListItemOutput output = new(m_RegionItem!);
        Assert.Multiple(() =>
        {
            Assert.That(output.Name, Is.EqualTo(m_RegionItem!.Name));
            Assert.That(output.RegionId, Is.EqualTo(m_RegionItem!.RegionID));
        });
    }

    [Test]
    public void TemplateRegionListItemOutputToString()
    {
        RegionTemplateListItemOutput output = new(m_RegionItem!);
        var sb = new StringBuilder();
        sb.AppendLine("name: " + ValidTemplateRegionName);
        sb.AppendLine("regionId: " + ValidTemplateRegionId);
        Assert.That(output.ToString(), Is.EqualTo(sb.ToString()));
    }
}
