using Unity.Services.DeploymentApi.Editor;

namespace Unity.Services.CloudContentDelivery.Authoring.Core.Model
{
    public interface IBucket : IDeploymentItem, ITypedItem
    {
        string Id { get; }
        new float Progress { get; set; }
    }
}
