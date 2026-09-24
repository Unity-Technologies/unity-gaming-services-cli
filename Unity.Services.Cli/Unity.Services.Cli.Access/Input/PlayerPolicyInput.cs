using System.CommandLine;
using Unity.Services.Cli.Common.Input;

namespace Unity.Services.Cli.Access.Input;

public class PlayerPolicyInput : AccessInput
{
    public static readonly Argument<string> PlayerIdArgument = new(
        name: "player-id", description: "The ID of the player");

    [InputBinding(nameof(PlayerIdArgument))]
    public string? PlayerId { get; set; }
}
