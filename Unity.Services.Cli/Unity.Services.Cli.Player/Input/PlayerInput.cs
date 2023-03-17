using System.CommandLine;
using Unity.Services.Cli.Common.Input;

namespace Unity.Services.Cli.Player.Input;

public class PlayerInput : CommonInput
{
    public static readonly Argument<string> PlayerIdArgument =
        new("player-id", "ID of the player");

    [InputBinding(nameof(PlayerIdArgument))]
    public string? PlayerId { get; set; }
}
