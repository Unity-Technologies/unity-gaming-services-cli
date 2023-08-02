using Unity.Services.Leaderboards.Authoring.Core.Model;

namespace Unity.Services.Cli.Leaderboards.Deploy;

public interface ILeaderboardsConfigLoader
{
    Task<IReadOnlyList<LeaderboardConfig>> LoadConfigsAsync(
        IReadOnlyCollection<string> paths,
        CancellationToken cancellationToken);
}
