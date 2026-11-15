using Newtonsoft.Json.Linq;
using Unity.Services.Cli.Scheduler.Handlers;
using Unity.Services.Gateway.SchedulerApiV1.Generated.Model;

namespace Unity.Services.Cli.Scheduler.UnitTest.Handlers;

[TestFixture]
class SchedulerListHandlerTests
{
    [TestCase(ScheduleStatus.Active, "active")]
    [TestCase(ScheduleStatus.Inactive, "inactive")]
    [TestCase(ScheduleStatus.Expired, "expired")]
    public void ScheduleItem_IncludesIdAndStatus(ScheduleStatus status, string expected)
    {
        var id = Guid.NewGuid();
        var config = new ScheduleConfig(id, "name", "event", "recurring", "0 * * * *", 1, "{}", status: status);

        var item = new SchedulerListHandler.ScheduleItem(config);

        Assert.Multiple(() =>
        {
            Assert.That(item.Id, Is.EqualTo(id.ToString()));
            Assert.That(item.Status, Is.EqualTo(expected));
            Assert.That(item.ToString(), Does.Contain($"status: {expected}"));
        });
    }

    [Test]
    public void ScheduleItem_UnsetBoundsAreNull()
    {
        var config = new ScheduleConfig(Guid.NewGuid(), "name", "event", "recurring", "0 * * * *", 1, "{}");

        var item = new SchedulerListHandler.ScheduleItem(config);

        Assert.Multiple(() =>
        {
            Assert.That(item.StartAt, Is.Null);
            Assert.That(item.EndAt, Is.Null);
        });
    }

    [Test]
    public void ScheduleItem_KeepsBounds()
    {
        var start = new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc);
        var end = new DateTime(2026, 2, 3, 4, 5, 6, DateTimeKind.Utc);
        var config = new ScheduleConfig(Guid.NewGuid(), "name", "event", "interval", "PT1H", 1, "{}", start, end);

        var item = new SchedulerListHandler.ScheduleItem(config);

        Assert.Multiple(() =>
        {
            Assert.That(item.StartAt, Is.EqualTo(start));
            Assert.That(item.EndAt, Is.EqualTo(end));
        });
    }
}
