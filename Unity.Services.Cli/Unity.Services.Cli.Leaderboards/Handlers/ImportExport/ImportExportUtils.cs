using System.Text.Json;
using Unity.Services.Cli.Common.Input;
using Unity.Services.Cli.Common.Utils;
using Unity.Services.Gateway.LeaderboardApiV1.Generated.Model;

namespace Unity.Services.Cli.Leaderboards.Handlers.ImportExport;

static class ImportExportUtils
{
    public static string ToRequestBody(this UpdatedLeaderboardConfig config)
    {
        return JsonSerializer.Serialize(config);
    }
}
