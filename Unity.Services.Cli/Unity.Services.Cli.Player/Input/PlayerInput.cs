using System.CommandLine;
using Unity.Services.Cli.Common.Input;

namespace Unity.Services.Cli.Player.Input;

public class PlayerInput : CommonInput
{
    public static readonly Argument<string> PlayerIdArgument =
        new("player-id", "ID of the player");

    public static readonly Option<int> PlayersLimitOption =
        new (new[] { "-l", "-limit"}, "The limit number of players to return");

    public static readonly Option<string> PlayersPageOption =
        new (new[] { "-n", "--next-page", "-page" }, "The next page token. If not set, returns players without any offset");

    [InputBinding(nameof(PlayerIdArgument))]
    public string? PlayerId { get; set; }

    [InputBinding(nameof(PlayersLimitOption))]
    public int Limit { get; set; }

    [InputBinding(nameof(PlayersPageOption))]
    public string? PlayersPage { get; set; }
}
