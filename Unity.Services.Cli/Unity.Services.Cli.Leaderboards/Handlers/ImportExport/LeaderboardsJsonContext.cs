using System.Text.Json.Serialization;
using Unity.Services.Gateway.LeaderboardApiV1.Generated.Model;

namespace Unity.Services.Cli.Leaderboards.Handlers.ImportExport;

[JsonSerializable(typeof(UpdatedLeaderboardConfig))]
partial class LeaderboardsJsonContext : JsonSerializerContext
{
}
