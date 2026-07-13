using System.CommandLine;
using Unity.Services.Cli.Common.Input;

namespace Unity.Services.Cli.Leaderboards.Input;

public class BucketScoresInput : PaginatedLeaderboardInput
{
    public static readonly Argument<string> BucketIdArgument = new("bucket-id", "The bucket ID");

    [InputBinding(nameof(BucketIdArgument))]
    public string? BucketId { get; set; }
}
