using System.Runtime.Serialization;

namespace Unity.Services.Leaderboards.Authoring.Core.Model
{
    public enum Strategy
    {
        [EnumMember(Value = "score")]
        Score = 1,

        [EnumMember(Value = "rank")]
        Rank = 2,

        [EnumMember(Value = "percent")]
        Percent = 3,
    }
}
