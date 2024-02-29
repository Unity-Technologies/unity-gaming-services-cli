using System.Runtime.Serialization;
using Unity.Services.DeploymentApi.Editor;

namespace Unity.Services.ModuleTemplate.Authoring.Core.Model
{
    public interface IResource
    {
        //TODO: Try not to leak non-human readable IDs to users (non-human readable like GUIDS, UUIDs)
        // Default to using the file as the ID, and allow overrides
        string Id { get; }
        string Name { get; }

        string AStrValue { get; set; }

        NestedObject NestedObj{ get; set; }
    }

    public interface IResourceDeploymentItem : IDeploymentItem, ITypedItem
    {
        new float Progress { get; set; }

        //TODO: Rename to match your model (e.g. script, entry, pool, etc)
        IResource Resource { get; set; }
    }

    [DataContract]
    public class NestedObject
    {
        [DataMember]
        public bool NestedObjectBoolean { get; set; }
        [DataMember]
        public string NestedObjectString { get; set; }
    }
}
