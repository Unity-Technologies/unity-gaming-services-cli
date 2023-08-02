using System.Runtime.Serialization;

namespace Unity.Services.Leaderboards.Authoring.Core.Model
{
    public enum UpdateType
    {
        [EnumMember(Value = "keepBest")]
        KeepBest = 1,

        [EnumMember(Value = "keepLatest")]
        KeepLatest = 2,

        [EnumMember(Value = "aggregate")]
        Aggregate = 3,
    }
}
