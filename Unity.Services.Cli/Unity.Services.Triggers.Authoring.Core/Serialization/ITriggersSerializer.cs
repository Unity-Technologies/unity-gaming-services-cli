using System.Collections.Generic;
using Unity.Services.Triggers.Authoring.Core.Model;

namespace Unity.Services.Triggers.Authoring.Core.Serialization
{
    public interface ITriggersSerializer
    {
        string Serialize(IList<ITriggerConfig> config);
    }
}
