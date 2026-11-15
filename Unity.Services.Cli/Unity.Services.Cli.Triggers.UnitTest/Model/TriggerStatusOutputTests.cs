using Newtonsoft.Json.Linq;
using NUnit.Framework;
using Unity.Services.Cli.Triggers.Model;
using Unity.Services.Gateway.TriggersApiV1.Generated.Model;

namespace Unity.Services.Cli.Triggers.UnitTest.Model;

[TestFixture]
class TriggerStatusOutputTests
{
    [TestCase(TriggerStatus.Active, "active")]
    [TestCase(TriggerStatus.Inactive, "inactive")]
    public void GetTriggerOutput_IncludesStatus(TriggerStatus status, string expected)
    {
        var config = new TriggerConfig(
            Guid.NewGuid(), DateTime.UtcNow, DateTime.UtcNow, "name", Guid.NewGuid(), Guid.NewGuid(),
            "eventType", "cloud-code", "urn:ugs:cloud-code:script", status: status);

        var json = JObject.Parse(new GetTriggerOutput(config).ToString());

        Assert.That(json["status"]!.Value<string>(), Is.EqualTo(expected));
    }

    [Test]
    public void ListTriggersOutput_IncludesStatusForEachTrigger()
    {
        var items = new[]
        {
            new TriggerConfigListItem(
                Guid.NewGuid(), DateTime.UtcNow, DateTime.UtcNow, "a", "eventType", "cloud-code", "urn:a",
                status: TriggerStatus.Active),
            new TriggerConfigListItem(
                Guid.NewGuid(), DateTime.UtcNow, DateTime.UtcNow, "b", "eventType", "cloud-code", "urn:b",
                status: TriggerStatus.Inactive),
        };

        var json = JArray.Parse(new ListTriggersOutput(items).ToString());

        Assert.That(json.Select(t => t["status"]!.Value<string>()), Is.EqualTo(new[] { "active", "inactive" }));
    }
}
