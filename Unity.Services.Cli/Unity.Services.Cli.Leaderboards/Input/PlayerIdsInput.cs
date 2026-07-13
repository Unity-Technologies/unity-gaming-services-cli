using System.CommandLine;
using Unity.Services.Cli.Common.Input;

namespace Unity.Services.Cli.Leaderboards.Input;

public class PlayerIdsInput : LeaderboardIdInput
{
    public static readonly Option<string> PlayerIdsOption = new Option<string>("--player-ids", "Comma-separated list of player IDs") { IsRequired = true };

    [InputBinding(nameof(PlayerIdsOption))]
    public string? PlayerIds { get; set; }
}
