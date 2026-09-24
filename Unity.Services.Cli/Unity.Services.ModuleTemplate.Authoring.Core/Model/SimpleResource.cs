using System.Runtime.Serialization;

namespace Unity.Services.ModuleTemplate.Authoring.Core.Model
{
    [DataContract]
    public class SimpleResource
    {
        [DataMember]
        public string Id { get; set; }
        [DataMember]
        public string Name { get; set; }
        [DataMember]
        public string AStrValue { get; set; }
        [DataMember]
        public NestedObject NestedObj { get; set; }
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
