using Microsoft.Extensions.Logging;
using Unity.Services.Cli.Common.Utils;
using Unity.Services.Cli.Authoring.Compression;
using Unity.Services.Cli.Authoring.Import;
using Unity.Services.Cli.RemoteConfig.Service;
using Unity.Services.Cli.RemoteConfig.Types;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Unity.Services.Cli.Authoring.Model;
using Unity.Services.Cli.Authoring.Templates;
using Unity.Services.Cli.RemoteConfig.Model;
using Unity.Services.Cli.Common.Exceptions;
using Unity.Services.Cli.Lobby.Handlers.Config;
using Unity.Services.Cli.RemoteConfig.Exceptions;

namespace Unity.Services.Cli.Lobby.Handlers.ImportExport;

class LobbyImporter : BaseImporter<LobbyConfig>
{
    readonly IRemoteConfigService m_RemoteConfigService;
    readonly IFileTemplate m_ConfigSchema;

    public LobbyImporter(
        IRemoteConfigService remoteConfigService,
        IZipArchiver zipArchiver,
        IUnityEnvironment unityEnvironment,
        ILogger logger)
        : base(
        zipArchiver,
        unityEnvironment,
        logger)
    {
        m_RemoteConfigService = remoteConfigService;
        m_ConfigSchema = new ConfigSchema();
    }

    protected override string FileName => LobbyConstants.ZipName;
    protected override string EntryName => LobbyConstants.EntryName;

    protected override async Task CreateConfigAsync(
        string projectId,
        string environmentId,
        LobbyConfig config,
        CancellationToken cancellationToken)
    {
        string json = config.Config.ToString();
        object newConfig = JsonConvert.DeserializeObject(json)!;
        ConfigValue value = new ConfigValue(LobbyConstants.ConfigKey, RemoteConfig.Types.ValueType.Json, newConfig);

        string configId = await m_RemoteConfigService.CreateConfigAsync(projectId, environmentId, LobbyConstants.ConfigType, new[] { value }, cancellationToken);

        // We can call the UpdateConfigAsync method to both 1) apply the schema and 2) update the config with the new
        // schema ID value.
        config.Id = configId;
        await UpdateConfigAsync(projectId, environmentId, config, cancellationToken);
    }

    protected override async Task UpdateConfigAsync(string projectId, string environmentId, LobbyConfig config, CancellationToken cancellationToken)
    {
        string configId = config.Id;

        // Configs should already have the schema applied, but we do this as a safeguard because if they don't, the
        // subsequent update request will fail.
        await ApplySchema(projectId, configId, cancellationToken);

        UpdateConfigRequest request = new UpdateConfigRequest
        {
            Type = LobbyConstants.ConfigType,
            Value = new object[] {
                new RemoteConfigResponse.ConfigValue
                {
                    Key = LobbyConstants.ConfigKey,
                    Type = RemoteConfig.Types.ValueType.Json.ToString().ToLower(),
                    SchemaId = LobbyConstants.SchemaId,
                    Value = config.Config
                }
            }
        };

        await m_RemoteConfigService.UpdateConfigAsync(
            projectId,
            configId,
            JsonConvert.SerializeObject(request,
                new JsonSerializerSettings { ContractResolver = new CamelCasePropertyNamesContractResolver() }),
            cancellationToken);
    }

    protected override ImportExportEntry<LobbyConfig> ToImportExportEntry(LobbyConfig value)
    {
        return new ImportExportEntry<LobbyConfig>(value.Id.GetHashCode(), LobbyConstants.ConfigDisplayName, value);
    }

    protected override Task DeleteConfigAsync(
        string projectId,
        string environmentId,
        LobbyConfig config,
        CancellationToken cancellationToken)
    {
        // It's not safe to delete a lobby configuration from the CLI yet because the CLI's creation and deletion happen
        // concurrently, so a user will experience errors if the existing config is not deleted before the new one is
        // created. However, this method should never be called; see CreateState for more details.
        throw new CliException("Unable to delete existing configuration.  This is not supported by the CLI.  Use an in-place upgrade (no --reconcile flag) instead.", ExitCode.HandledError);
    }

    protected override async Task<IEnumerable<LobbyConfig>> ListConfigsAsync(string projectId, string environmentId, CancellationToken cancellationToken)
    {
        var response = await m_RemoteConfigService.GetAllConfigsFromEnvironmentAsync(
            projectId,
            environmentId,
            LobbyConstants.ConfigType,
            cancellationToken);

        LobbyConfig.TryParse(response, out LobbyConfig? config);
        if (config == null)
        {
            return new List<LobbyConfig>();
        }

        return new List<LobbyConfig>{ config };
    }

    protected override ImportState<LobbyConfig> CreateState(IEnumerable<LobbyConfig> localConfigs, IEnumerable<LobbyConfig> remoteConfigs)
    {
        var toCreate = new List<ImportExportEntry<LobbyConfig>>();
        var toUpdate = new List<ImportExportEntry<LobbyConfig>>();

        if (localConfigs.Any())
        {
            var newConfig = ToImportExportEntry(localConfigs.First());

            if (remoteConfigs.Any())
            {
                // Remote Config will reject the request if you try to create an additional "Lobby"-type config, so we need
                // to use the existing config's ID and treat this as an update.
                localConfigs.First().Id = remoteConfigs.First().Id;
                toUpdate.Add(newConfig);
            }
            else
            {
                toCreate.Add(newConfig);
            }
        }

        // This list must remain empty. If the remote config is marked for deletion, it may cause the coinciding create
        // to fail because the create could come first, which would also trigger the Remote Config error for duplicate
        // "Lobby"-type configs. Fortunately, in Lobby's case, just updating the existing config is functionally the same
        // as the intended effect of the --reconcile flag (deleting, then re-creating), so that's what we do instead.
        var toDelete = new List<ImportExportEntry<LobbyConfig>>();

        return new ImportState<LobbyConfig>(toCreate, toUpdate, toDelete);
    }

    async Task ApplySchema(string projectId, string configId, CancellationToken cancellationToken)
    {
        try
        {
            await m_RemoteConfigService.ApplySchemaAsync(projectId, configId, m_ConfigSchema.FileBodyText, cancellationToken);
        }
        catch (ApiException) // Because it's an internal API, we hide any schema-related HTTP exceptions from the user.
        {
            throw new CliException("An error occurred while importing the Lobby configuration.", ExitCode.HandledError);
        }
    }
}
