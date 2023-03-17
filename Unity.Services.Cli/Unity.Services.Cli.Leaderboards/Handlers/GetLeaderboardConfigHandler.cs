using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Unity.Services.Cli.Common.Console;
using Unity.Services.Cli.Common.Logging;
using Unity.Services.Cli.Common.Utils;
using Unity.Services.Cli.Leaderboards.Input;
using Unity.Services.Cli.Leaderboards.Service;
using Unity.Services.Gateway.LeaderboardApiV1.Generated.Model;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Unity.Services.Cli.Leaderboards.Handlers;

static class GetLeaderboardConfigHandler
{
    public static async Task GetLeaderboardConfigAsync(
        LeaderboardIdInput input,
        IUnityEnvironment unityEnvironment,
        ILeaderboardsService leaderboardsService,
        ILogger logger,
        ILoadingIndicator loadingIndicator,
        CancellationToken cancellationToken)
    {
        await loadingIndicator.StartLoadingAsync(
            "Fetching leaderboard info...",
            _ => GetLeaderboardConfigAsync(input, unityEnvironment, leaderboardsService, logger, cancellationToken));
    }

    internal static async Task GetLeaderboardConfigAsync(
        LeaderboardIdInput input,
        IUnityEnvironment unityEnvironment,
        ILeaderboardsService leaderboardsService,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var environmentId = await unityEnvironment.FetchIdentifierAsync();
        var projectId = input.CloudProjectId!;
        var response = await leaderboardsService.GetLeaderboardAsync(
            projectId, environmentId, input.LeaderboardId!, cancellationToken);
        var leaderboard = JsonConvert.DeserializeObject<UpdatedLeaderboardConfig>(response.RawContent);

        logger.LogResultValue(ToString(leaderboard!));
    }

    internal static string ToString(UpdatedLeaderboardConfig config)
    {
        var serializer = new SerializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .DisableAliases()
            .Build();
        return serializer.Serialize(config);
    }
}
