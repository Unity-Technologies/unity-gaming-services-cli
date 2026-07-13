using System.CommandLine;
using Unity.Services.Cli.Common.Input;

namespace Unity.Services.Cli.Leaderboards.Input;

public class PaginatedLeaderboardInput : LeaderboardIdInput
{
    public static readonly Option<int?> OffsetOption = new Option<int?>("--offset", "The number of entries to skip");
    [InputBinding(nameof(OffsetOption))]
    public int? Offset { get; set; }

    public static readonly Option<int?> LimitOption = new Option<int?>("--limit", "The number of results to return. Defaults to 10");
    [InputBinding(nameof(LimitOption))]
    public int? Limit { get; set; }

    public static readonly Option<string?> TierOption = new("--tier", "Filter by tier ID");

    [InputBinding(nameof(TierOption))]
    public string? TierId { get; set; }
}
