using System.CommandLine;
using Unity.Services.Cli.Common.Input;

namespace Unity.Services.Cli.Leaderboards.Input;

class ListLeaderboardInput : CommonInput
{
    public static readonly Option<string?> CursorOption = new Option<string?>("--cursor", "The ID of the leaderboard that listing should start after, i.e. the last leaderboard returned from the previous page when paging");
    [InputBinding(nameof(CursorOption))]
    public string? Cursor { get; set; }

    public static readonly Option<int?> LimitOption = new Option<int?>("--limit", "The number of leaderboards to return. Defaults to 10");
    [InputBinding(nameof(LimitOption))]
    public int? Limit { get; set; }
}
