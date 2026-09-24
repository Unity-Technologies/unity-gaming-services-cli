using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Unity.Services.Cli.Authoring.Model;
using Unity.Services.Cli.Authoring.Templates;
using Unity.Services.Cli.Common.Exceptions;
using Unity.Services.Cli.Lobby.Handlers;
using Unity.Services.Cli.Lobby.Handlers.Config;
using Unity.Services.Cli.RemoteConfig.Exceptions;
using Unity.Services.Cli.RemoteConfig.Service;
using Unity.Services.Cli.RemoteConfig.Types;
using Unity.Services.DeploymentApi.Editor;
using UpdateConfigRequest = Unity.Services.Cli.RemoteConfig.Model.UpdateConfigRequest;

namespace Unity.Services.Cli.Lobby.Deploy;

class LobbyDeploymentHandler
{
    readonly IRemoteConfigService m_RemoteConfigService;
    readonly IFileTemplate m_ConfigSchema;
    readonly IFileTemplate m_ConfigSchemaV2;
    readonly IFileTemplate m_ConfigSchemaV3;

    static readonly JsonSerializerSettings k_CamelCaseSettings = new()
    {
        ContractResolver = new CamelCasePropertyNamesContractResolver()
    };

    public LobbyDeploymentHandler(IRemoteConfigService remoteConfigService)
    {
        m_RemoteConfigService = remoteConfigService;
        m_ConfigSchema = new ConfigSchema();
        m_ConfigSchemaV2 = new ConfigSchemaV2();
        m_ConfigSchemaV3 = new ConfigSchemaV3();
    }

    public async Task<DeploymentResult> DeployAsync(
        LobbyDeploymentItem? item,
        string projectId,
        string environmentId,
        bool dryRun,
        bool reconcile,
        CancellationToken cancellationToken)
    {
        var updated = new List<IDeploymentItem>();
        var deleted = new List<IDeploymentItem>();
        var created = new List<IDeploymentItem>();
        var deployed = new List<IDeploymentItem>();
        var failed = new List<IDeploymentItem>();

        List<LobbyConfig> remoteConfigs;
        try
        {
            remoteConfigs = await ListConfigsAsync(projectId, environmentId, cancellationToken);
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            var failedItem = item ?? new LobbyDeploymentItem("Remote");
            failedItem.Status = new DeploymentStatus(
                "Failed to deploy", e.Message, SeverityLevel.Error);
            failed.Add(failedItem);
            return new DeploymentResult(updated, deleted, created, deployed, failed, dryRun);
        }

        var remoteConfig = remoteConfigs.FirstOrDefault();

        if (item?.Config == null)
        {
            if (reconcile)
            {
                await ReconcileDeleteAsync(
                    remoteConfig, projectId, dryRun, deleted, deployed, failed, cancellationToken);
            }

            return new DeploymentResult(updated, deleted, created, deployed, failed, dryRun);
        }

        var localConfig = item.Config;
        bool isUpdate = remoteConfig != null;
        if (isUpdate)
        {
            localConfig.Id = remoteConfig!.Id;
        }

        if (!dryRun)
        {
            try
            {
                if (isUpdate)
                {
                    await UpdateConfigAsync(projectId, environmentId, localConfig, cancellationToken);
                }
                else
                {
                    await CreateConfigAsync(projectId, environmentId, localConfig, cancellationToken);
                }
            }
            catch (Exception e) when (e is not OperationCanceledException)
            {
                item.Status = new DeploymentStatus(
                    "Failed to deploy", e.Message, SeverityLevel.Error);
                failed.Add(item);
                return new DeploymentResult(updated, deleted, created, deployed, failed, dryRun);
            }
        }

        item.Progress = 100f;
        var action = isUpdate ? Statuses.Updated : Statuses.Created;
        item.Status = new DeploymentStatus(Statuses.Deployed, action, SeverityLevel.Success);

        if (isUpdate)
        {
            updated.Add(item);
        }
        else
        {
            created.Add(item);
        }

        deployed.Add(item);
        return new DeploymentResult(updated, deleted, created, deployed, failed, dryRun);
    }

    async Task ReconcileDeleteAsync(
        LobbyConfig? remoteConfig,
        string projectId,
        bool dryRun,
        List<IDeploymentItem> deleted,
        List<IDeploymentItem> deployed,
        List<IDeploymentItem> failed,
        CancellationToken cancellationToken)
    {
        if (remoteConfig == null)
        {
            return;
        }

        var deleteItem = new LobbyDeploymentItem("Remote");

        if (!dryRun)
        {
            try
            {
                await m_RemoteConfigService.DeleteConfigAsync(
                    projectId, remoteConfig.Id, LobbyConstants.ConfigType, cancellationToken);
            }
            catch (Exception e) when (e is not OperationCanceledException)
            {
                deleteItem.Status = new DeploymentStatus(
                    "Failed to deploy", e.Message, SeverityLevel.Error);
                failed.Add(deleteItem);
                return;
            }
        }

        deleteItem.Progress = 100f;
        deleteItem.Status = new DeploymentStatus(
            Statuses.Deployed, Statuses.Deleted, SeverityLevel.Success);

        deleted.Add(deleteItem);
        deployed.Add(deleteItem);
    }

    internal async Task<List<LobbyConfig>> ListConfigsAsync(
        string projectId,
        string environmentId,
        CancellationToken cancellationToken)
    {
        var response = await m_RemoteConfigService.GetAllConfigsFromEnvironmentAsync(
            projectId, environmentId, LobbyConstants.ConfigType, cancellationToken);

        if (LobbyConfig.TryParse(response, out var config))
        {
            return new List<LobbyConfig> { config! };
        }

        return new List<LobbyConfig>();
    }

    async Task CreateConfigAsync(
        string projectId,
        string environmentId,
        LobbyConfig config,
        CancellationToken cancellationToken)
    {
        var json = config.Config.ToString();
        var newConfig = JsonConvert.DeserializeObject(json)!;
        var value = new ConfigValue(LobbyConstants.ConfigKey, RemoteConfig.Types.ValueType.Json, newConfig);

        var configId = await m_RemoteConfigService.CreateConfigAsync(
            projectId, environmentId, LobbyConstants.ConfigType, new[] { value }, cancellationToken);

        config.Id = configId;
        await UpdateConfigAsync(projectId, environmentId, config, cancellationToken);
    }

    async Task UpdateConfigAsync(
        string projectId,
        string environmentId,
        LobbyConfig config,
        CancellationToken cancellationToken)
    {
        var schemaId = config.SchemaId;

        await ApplySchemaAsync(projectId, config.Id, schemaId, cancellationToken);

        var request = new UpdateConfigRequest
        {
            Type = LobbyConstants.ConfigType,
            Value = new object[]
            {
                new LobbyConfigValue
                {
                    Key = LobbyConstants.ConfigKey,
                    Type = RemoteConfig.Types.ValueType.Json.ToString().ToLower(),
                    SchemaId = schemaId,
                    Value = config.Config
                }
            }
        };

        await m_RemoteConfigService.UpdateConfigAsync(
            projectId,
            config.Id,
            JsonConvert.SerializeObject(request, k_CamelCaseSettings),
            cancellationToken);
    }

    async Task ApplySchemaAsync(
        string projectId,
        string configId,
        string schemaId,
        CancellationToken cancellationToken)
    {
        var schemaBody = schemaId switch
        {
            LobbyConstants.SchemaIdV2 => m_ConfigSchemaV2.FileBodyText,
            LobbyConstants.SchemaIdV3 => m_ConfigSchemaV3.FileBodyText,
            _ => m_ConfigSchema.FileBodyText,
        };

        try
        {
            await m_RemoteConfigService.ApplySchemaAsync(
                projectId, configId, schemaBody, cancellationToken);
        }
        catch (ApiException)
        {
            throw new CliException(
                "An error occurred while deploying the Lobby configuration.",
                ExitCode.HandledError);
        }
    }
}
