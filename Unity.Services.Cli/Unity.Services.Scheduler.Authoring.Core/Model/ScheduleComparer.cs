using System.Collections.Generic;

namespace Unity.Services.Scheduler.Authoring.Core.Model
{
    public class ScheduleComparer : IEqualityComparer<IScheduleConfig>
    {
        public bool Equals(IScheduleConfig x, IScheduleConfig y)
        {
            if (ReferenceEquals(x, y)) return true;
            if (ReferenceEquals(x, null)) return false;
            if (ReferenceEquals(y, null)) return false;
            if (x.GetType() != y.GetType()) return false;
            return x.Name == y.Name;
        }

        public int GetHashCode(IScheduleConfig obj)
        {
            return (obj.Name != null ? obj.Name.GetHashCode() : 0);
        }
    }
}
