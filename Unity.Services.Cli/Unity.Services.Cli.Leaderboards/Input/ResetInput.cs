using System.CommandLine;
using Unity.Services.Cli.Common.Input;

namespace Unity.Services.Cli.Leaderboards.Input;

internal class ResetInput : LeaderboardIdInput
{
    private const string k_ArchiveDescription =
        "Whether or not to archive the current set of scores before resetting the leaderboard. ";
    public static readonly Option<bool> ResetArchiveArgument = new(new[]
    {
        "-a",
        "--archive"
    }, k_ArchiveDescription);

    [InputBinding(nameof(ResetArchiveArgument))]
    public bool? Archive { get; set; }
}
