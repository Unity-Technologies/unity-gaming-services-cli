using Newtonsoft.Json;
using Unity.Services.DeploymentApi.Editor;
using Unity.Services.Leaderboards.Authoring.Core.IO;
using Unity.Services.Leaderboards.Authoring.Core.Model;

namespace Unity.Services.Cli.Leaderboards.Deploy;

class LeaderboardsConfigLoader : ILeaderboardsConfigLoader
{
    readonly IFileSystem m_FileSystem;

    public LeaderboardsConfigLoader(IFileSystem fileSystem)
    {
        m_FileSystem = fileSystem;
    }

    public async Task<IReadOnlyList<LeaderboardConfig>> LoadConfigsAsync(IReadOnlyCollection<string> paths, CancellationToken cancellationToken)
    {
        var leaderboards = new List<LeaderboardConfig>();
        var serializationSettings = LeaderboardConfigFile.GetSerializationSettings();
        foreach (var path in paths)
        {
            var fileName = Path.GetFileNameWithoutExtension(path);
            var lb = new LeaderboardConfig(fileName, fileName);
            lb.Path = path;
            try
            {
                var content = await m_FileSystem.ReadAllText(path, cancellationToken);
                var leaderboardConfigFile = JsonConvert.DeserializeObject<LeaderboardConfigFile>(
                    content,
                    serializationSettings)!;

                lb = FromFile(leaderboardConfigFile, path);
                lb.Status = new DeploymentStatus("Loaded");
            }
            catch (Exception ex)
            {
                lb.Status = new DeploymentStatus(
                    "Failed to Load",
                    $"Error reading file: {ex.Message}",
                    SeverityLevel.Error);
            }
            leaderboards.Add(lb);
        }

        return leaderboards;
    }

    static LeaderboardConfig FromFile(LeaderboardConfigFile config, string path)
    {
        var lb = new LeaderboardConfig(
            config.Id ?? Path.GetFileNameWithoutExtension(path),
            config.Name,
            config.SortOrder,
            config.UpdateType);
        lb.BucketSize = config.BucketSize;
        lb.ResetConfig = config.ResetConfig;
        lb.TieringConfig = config.TieringConfig;
        lb.Path = path;
        return lb;
    }
}
