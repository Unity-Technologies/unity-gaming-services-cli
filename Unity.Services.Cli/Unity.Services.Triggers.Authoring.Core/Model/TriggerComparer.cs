using System.Collections.Generic;

namespace Unity.Services.Triggers.Authoring.Core.Model
{
    public class TriggerComparer : IEqualityComparer<ITriggerConfig>
    {
        public bool Equals(ITriggerConfig x, ITriggerConfig y)
        {
            if (ReferenceEquals(x, y)) return true;
            if (ReferenceEquals(x, null)) return false;
            if (ReferenceEquals(y, null)) return false;
            if (x.GetType() != y.GetType()) return false;
            return x.Name == y.Name;
        }

        public int GetHashCode(ITriggerConfig obj)
        {
            return (obj.Name != null ? obj.Name.GetHashCode() : 0);
        }
    }
}
