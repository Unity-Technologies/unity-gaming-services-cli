using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using Unity.Services.DeploymentApi.Editor;

namespace Unity.Services.ModuleTemplate.Authoring.Core.Model
{
    [DataContract]
    public class SimpleResource : IResource
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
}
