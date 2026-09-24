using System.Globalization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using Unity.Services.Cli.Scheduler.Deploy;

namespace Unity.Services.Cli.Scheduler.UnitTest.Deploy;

[TestFixture]
public class ScheduleConfigFileTests
{
    [Test]
    public void HelpBodyText_IsDeterministic()
    {
        var template = new ScheduleConfigFile();
        var first = template.HelpBodyText;
        var second = template.HelpBodyText;
        Assert.That(first, Is.EqualTo(second));
    }

    [Test]
    public void HelpBodyText_ContainsFixedUtcDate()
    {
        var template = new ScheduleConfigFile();
        Assert.That(template.HelpBodyText, Does.Contain("2099-01-01T00:00:00.000Z"));
    }

    [Test]
    public void HelpBodyText_DiffersFromFileBodyText()
    {
        var template = new ScheduleConfigFile();
        Assert.That(template.HelpBodyText, Is.Not.EqualTo(template.FileBodyText));
    }

    [Test]
    public void FileBodyText_ContainsDateBoundsAndExplicitNullExamples()
    {
        var template = new ScheduleConfigFile();
        var configs = JObject.Parse(template.FileBodyText)["Configs"]!;

        Assert.That(configs["Schedule1"]!["StartAt"]!.Type, Is.EqualTo(JTokenType.Date));
        Assert.That(configs["Schedule1"]!["EndAt"]!.Type, Is.EqualTo(JTokenType.Date));
        Assert.That(configs["Schedule2"]!["StartAt"]!.Type, Is.EqualTo(JTokenType.Null));
        Assert.That(configs["Schedule2"]!["EndAt"]!.Type, Is.EqualTo(JTokenType.Null));
        Assert.That(configs["Schedule3"]!["StartAt"]!.Type, Is.EqualTo(JTokenType.Null));
        Assert.That(configs["Schedule3"]!["EndAt"]!.Type, Is.EqualTo(JTokenType.Null));
    }

    [Test]
    public void FileBodyText_OneTimeScheduleUsesUtcRfc3339Format()
    {
        var beforeCreation = DateTimeOffset.UtcNow.AddMinutes(59);
        var template = new ScheduleConfigFile();
        var loaded = JsonConvert.DeserializeObject<ScheduleConfigFile>(
            template.FileBodyText,
            ScheduleConfigFile.GetSerializationSettings());
        var schedule = loaded!.Entries.Single(entry => entry.Name == "Schedule2").Schedule;

        var parsed = DateTimeOffset.ParseExact(
            schedule!,
            "yyyy-MM-dd'T'HH:mm:ss.fffzzz",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None);

        Assert.Multiple(() =>
        {
            Assert.That(parsed.Offset, Is.EqualTo(TimeSpan.Zero));
            Assert.That(
                parsed,
                Is.GreaterThanOrEqualTo(beforeCreation)
            );
            Assert.That(parsed, Is.LessThanOrEqualTo(DateTimeOffset.UtcNow.AddHours(1)));
        });
    }

    [Test]
    public void HelpBodyText_OneTimeScheduleUsesFixedUtcRfc3339Format()
    {
        var template = new ScheduleConfigFile();
        var loaded = JsonConvert.DeserializeObject<ScheduleConfigFile>(
            template.HelpBodyText,
            ScheduleConfigFile.GetSerializationSettings());
        var schedule = loaded!.Entries.Single(entry => entry.Name == "Schedule2").Schedule;

        Assert.That(schedule, Is.EqualTo("2099-01-01T00:00:00.000Z"));
    }

    [TestCase("Schedule1", "recurring", "0 * * * *")]
    [TestCase("Schedule2", "one-time", null)]
    [TestCase("Schedule3", "interval", "3d")]
    public void FileBodyText_ContainsScheduleTypeExample(
        string configName,
        string expectedType,
        string? expectedSchedule)
    {
        var template = new ScheduleConfigFile();
        var config = JObject.Parse(template.FileBodyText)["Configs"]![configName]!;

        Assert.That(config["Type"]!.Value<string>(), Is.EqualTo(expectedType));
        Assert.That(
            config["Schedule"]!.Type,
            expectedSchedule is null
                ? Is.EqualTo(JTokenType.Date)
                : Is.EqualTo(JTokenType.String));
        if (expectedSchedule is not null)
        {
            Assert.That(config["Schedule"]!.Value<string>(), Is.EqualTo(expectedSchedule));
        }
    }
}
