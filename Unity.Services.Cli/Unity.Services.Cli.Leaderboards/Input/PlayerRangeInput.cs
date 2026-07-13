using System.CommandLine;
using Unity.Services.Cli.Common.Input;

namespace Unity.Services.Cli.Leaderboards.Input;

public class PlayerRangeInput : PlayerScoreInput
{
    public static readonly Option<int?> RangeLimitOption = new Option<int?>("--range-limit", "The number of entries on either side of the player to return. Defaults to 5");

    [InputBinding(nameof(RangeLimitOption))]
    public int? RangeLimit { get; set; }
}
