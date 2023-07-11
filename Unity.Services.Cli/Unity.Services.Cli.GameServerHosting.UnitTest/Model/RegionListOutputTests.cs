using System.Text;
using Unity.Services.Cli.GameServerHosting.Model;
using Unity.Services.Gateway.GameServerHostingApiV1.Generated.Model;

namespace Unity.Services.Cli.GameServerHosting.UnitTest.Model;

[TestFixture]
public class RegionListOutputTests
{
    [SetUp]
    public void SetUp()
    {
        m_Regions = new List<FleetRegionsTemplateListItem>
        {
            new(
                regionID: new Guid(ValidTemplateRegionId),
                name: ValidTemplateRegionName
            ),
            new(
                regionID: new Guid(ValidTemplateRegionId2),
                name: ValidTemplateRegionName2
            )
        };
    }

    List<FleetRegionsTemplateListItem>? m_Regions;

    [Test]
    public void ConstructTemplateRegionListOutputWithValidList()
    {
        RegionTemplateListOutput output = new(m_Regions!);
        Assert.That(output, Has.Count.EqualTo(m_Regions!.Count));
        for (var i = 0; i < output.Count; i++)
            Assert.Multiple(() =>
            {
                Assert.That(output[i].Name, Is.EqualTo(m_Regions[i].Name));
                Assert.That(output[i].RegionId, Is.EqualTo(m_Regions[i].RegionID));
            });
    }

    [Test]
    public void TemplateRegionListOutputToString()
    {
        RegionTemplateListOutput output = new(m_Regions!);
        var sb = new StringBuilder();
        foreach (var region in output)
        {
            sb.AppendLine("- name: " + region.Name);
            sb.AppendLine("  regionId: " + region.RegionId);
        }

        Assert.That(output.ToString(), Is.EqualTo(sb.ToString()));
    }
}
