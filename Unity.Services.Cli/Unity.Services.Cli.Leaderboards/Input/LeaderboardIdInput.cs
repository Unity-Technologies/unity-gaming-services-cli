using System.CommandLine;
using Unity.Services.Cli.Common.Input;

namespace Unity.Services.Cli.Leaderboards.Input;

public class LeaderboardIdInput : CommonInput
{
    const string k_JsonLeaderboardIdDescription = "leaderboard id to fetch or update";

    public static readonly Argument<string> RequestLeaderboardIdArgument = new("leaderboard-id", k_JsonLeaderboardIdDescription);

    [InputBinding(nameof(RequestLeaderboardIdArgument))]
    public string? LeaderboardId { get; set; }

    public static readonly Option<string?> VersionOption = new("--version", "Archived version ID");

    [InputBinding(nameof(VersionOption))]
    public string? VersionId { get; set; }
}
