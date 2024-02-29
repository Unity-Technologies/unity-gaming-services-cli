using Microsoft.Extensions.Logging;
using Unity.Services.Cli.Common.Console;
using Unity.Services.Cli.Common.Input;
using Unity.Services.Cli.Common.Logging;
using Unity.Services.Cli.Common.Utils;
using Unity.Services.Scheduler.Authoring.Core.Model;
using Unity.Services.Scheduler.Authoring.Core.Service;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Unity.Services.Cli.Scheduler.Handlers;

static class SchedulerListHandler
{
    public static async Task SchedulerListHandlerHandlerAsync(
        CommonInput input,
        IUnityEnvironment unityEnvironment,
        ISchedulerClient schedulerAdminClient,
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
        ISchedulerClient schedulerAdminClient,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var environmentId = await unityEnvironment.FetchIdentifierAsync(cancellationToken);
        var projectId = input.CloudProjectId!;
        await schedulerAdminClient.Initialize(environmentId, projectId, cancellationToken);
        var listResult = await schedulerAdminClient.List();

        var cliFriendlyList = listResult.Select(i => new ScheduleItem(i));
        logger.LogResultValue(cliFriendlyList);
    }

    class ScheduleItem
    {
        readonly IScheduleConfig  m_ServerModel;
        public string Name { get; }
        public string EventName { get; }
        public string ScheduleType { get; }
        public string Schedule { get; }
        public int PayloadVersion { get; }
        public string Payload { get; }

        public ScheduleItem(IScheduleConfig serverModel)
        {
            m_ServerModel = serverModel;
            Name = serverModel.Name;
            Schedule = serverModel.Schedule;
            EventName = serverModel.EventName;
            ScheduleType = serverModel.ScheduleType;
            PayloadVersion = serverModel.PayloadVersion;
            Payload = serverModel.Payload;
        }

        public override string ToString()
        {
            var serializer = new SerializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .DisableAliases()
                .Build();
            return serializer.Serialize(m_ServerModel);
        }
    }
}
