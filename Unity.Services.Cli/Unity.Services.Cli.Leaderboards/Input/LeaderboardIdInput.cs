using System.CommandLine;
using Unity.Services.Cli.Common.Input;

namespace Unity.Services.Cli.Leaderboards.Input;

internal class LeaderboardIdInput : CommonInput
{
    private const string k_JsonLeaderboardIdDescription = "leaderboard id to update";

    public static readonly Argument<string> RequestLeaderboardIdArgument = new("leaderboard-id", k_JsonLeaderboardIdDescription);

    [InputBinding(nameof(RequestLeaderboardIdArgument))]
    public string? LeaderboardId { get; set; }
}
