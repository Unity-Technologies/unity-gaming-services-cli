using System.CommandLine;
using Unity.Services.Cli.Common.Input;

namespace Unity.Services.Cli.Leaderboards.Input;

public class PurgePlayerInput : CommonInput
{
    public static readonly Argument<string> PlayerIdArgument = new("player-id", "The player ID");

    [InputBinding(nameof(PlayerIdArgument))]
    public string? PlayerId { get; set; }
}
