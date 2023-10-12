using Unity.Services.DeploymentApi.Editor;

namespace Unity.Services.Triggers.Authoring.Core.Model
{
    public interface ITriggerConfig : IDeploymentItem, ITypedItem
    {
        string Id { get; set; }
        new float Progress { get; set; }

        string EventType { get; }
        string ActionType { get; }
        string ActionUrn { get; }
    }
}
