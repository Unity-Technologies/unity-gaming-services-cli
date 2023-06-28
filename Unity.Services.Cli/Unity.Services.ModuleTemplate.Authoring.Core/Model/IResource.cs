using Unity.Services.DeploymentApi.Editor;

namespace Unity.Services.ModuleTemplate.Authoring.Core.Model
{
    public interface IResource : IDeploymentItem, ITypedItem
    {
        string Id { get; }
        new float Progress { get; set; }
    }
}
