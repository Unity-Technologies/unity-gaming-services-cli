using System.CommandLine;
using Unity.Services.Cli.Common.Input;

namespace Unity.Services.Cli.Access.Input;

public class PlayerPolicyListInput : CommonInput
{
    public static readonly Option<string?> PlayerIdOption = new(
        name: "--player-id", description: "The ID of the player. Omit to list all player policies.");

    [InputBinding(nameof(PlayerIdOption))]
    public string? PlayerId { get; set; }
}
