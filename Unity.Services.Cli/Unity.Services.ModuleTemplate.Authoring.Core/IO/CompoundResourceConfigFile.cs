using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Unity.Services.ModuleTemplate.Authoring.Core.Model;

namespace Unity.Services.ModuleTemplate.Authoring.Core.IO
{
    [Serializable]
    public class CompoundResourceConfigFile
    {
        public CompoundResourceConfigFile()
        {
            Resources = new Dictionary<string, ModuleTemplateResourceEntry>();
        }

        public Dictionary<string, ModuleTemplateResourceEntry> Resources { get; set; }
    }

    [DataContract]
    public class ModuleTemplateResourceEntry
    {
        [DataMember]
        public string Name { get; set; }
        [DataMember]
        public string UserFriendlyStringName { get; set; }
        [DataMember]
        public NestedObject NestedObj { get; set; }
    }
}
