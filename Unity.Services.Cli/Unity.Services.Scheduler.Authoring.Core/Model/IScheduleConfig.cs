using Unity.Services.DeploymentApi.Editor;

namespace Unity.Services.Scheduler.Authoring.Core.Model
{
    public interface IScheduleConfig : IDeploymentItem, ITypedItem
    {
        string Id { get; set; }
        string EventName { get; }
        string ScheduleType { get; }
        string Schedule { get; }
        int PayloadVersion { get; }
        string Payload { get; }

        new float Progress { get; set; }
        new string Path { get; set; }
    }
}
