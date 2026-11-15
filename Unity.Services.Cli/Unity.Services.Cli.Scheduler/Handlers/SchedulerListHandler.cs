using Microsoft.Extensions.Logging;
using Unity.Services.Cli.Common.Console;
using Unity.Services.Cli.Common.Input;
using Unity.Services.Cli.Common.Logging;
using Unity.Services.Cli.Common.Utils;
using Unity.Services.Cli.Scheduler.Service;
using Unity.Services.Gateway.SchedulerApiV1.Generated.Model;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Unity.Services.Cli.Scheduler.Handlers;

static class SchedulerListHandler
{
    public static async Task SchedulerListHandlerHandlerAsync(
        CommonInput input,
        IUnityEnvironment unityEnvironment,
        ISchedulerStatusClient schedulerAdminClient,
        ILogger logger,
        ILoadingIndicator loadingIndicator,
        CancellationToken cancellationToken)
    {
        await loadingIndicator.StartLoadingAsync(
            "Fetching scheduler resource list...",
            _ => SchedulerListAsync(input, unityEnvironment, schedulerAdminClient, logger, cancellationToken));
    }

    static async Task SchedulerListAsync(
        CommonInput input,
        IUnityEnvironment unityEnvironment,
        ISchedulerStatusClient schedulerAdminClient,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var environmentId = await unityEnvironment.FetchIdentifierAsync(cancellationToken);
        var projectId = input.CloudProjectId!;
        var listResult = await schedulerAdminClient.ListConfigs(cancellationToken);

        var cliFriendlyList = listResult.Select(i => new ScheduleItem(i));
        logger.LogResultValue(cliFriendlyList);
    }

    internal class ScheduleItem
    {
        public string Id { get; }
        public string Name { get; }
        public string EventName { get; }
        public string ScheduleType { get; }
        public string Schedule { get; }
        public int PayloadVersion { get; }
        public string Payload { get; }
        public DateTime? StartAt { get; }
        public DateTime? EndAt { get; }
        public string Status { get; }

        public ScheduleItem(ScheduleConfig serverModel)
        {
            Id = serverModel.Id.ToString();
            Name = serverModel.Name;
            Schedule = serverModel.Schedule;
            EventName = serverModel.EventName;
            ScheduleType = serverModel.Type;
            PayloadVersion = serverModel.PayloadVersion;
            Payload = serverModel.Payload;
            StartAt = serverModel.StartAt == default ? null : serverModel.StartAt;
            EndAt = serverModel.EndAt == default ? null : serverModel.EndAt;
            Status = serverModel.Status.ToString().ToLowerInvariant();
        }

        public override string ToString()
        {
            var serializer = new SerializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .DisableAliases()
                .Build();
            return serializer.Serialize(this);
        }
    }
}
