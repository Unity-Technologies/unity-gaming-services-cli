using Microsoft.Extensions.Logging;
using Unity.Services.Cli.Common.Utils;
using Unity.Services.Cli.Authoring.Compression;
using Unity.Services.Cli.Authoring.Import;
using Unity.Services.Cli.Authoring.Import.Input;
using Unity.Services.Cli.Authoring.Model;
using Unity.Services.Cli.Leaderboards.Service;
using Unity.Services.Gateway.LeaderboardApiV1.Generated.Client;
using Unity.Services.Gateway.LeaderboardApiV1.Generated.Model;

namespace Unity.Services.Cli.Leaderboards.Handlers.ImportExport;

class LeaderboardImporter : BaseImporter<UpdatedLeaderboardConfig>
{
    readonly ILeaderboardsService m_LeaderboardsService;

    public LeaderboardImporter(
        ILeaderboardsService leaderboardsService,
        IZipArchiver zipArchiver,
        IUnityEnvironment unityEnvironment,
        ILogger logger)
        : base(
        zipArchiver,
        unityEnvironment,
        logger)
    {
        m_LeaderboardsService = leaderboardsService;
    }

    protected override string FileName => LeaderboardConstants.ZipName;

    protected override string EntryName => LeaderboardConstants.EntryName;

    protected override async Task DeleteConfigAsync(
        string projectId,
        string environmentId,
        UpdatedLeaderboardConfig configToDelete,
        CancellationToken cancellationToken)
    {
        ValidateResponse(
            await m_LeaderboardsService.DeleteLeaderboardAsync(
                projectId,
                environmentId,
                configToDelete.Id,
                cancellationToken
            ),
            () => m_Logger.LogInformation("[{Name} : {Id}] successfully deleted", configToDelete.Name, configToDelete.Id),
            errorMsg => m_Logger.LogError("Failed to delete [{Name} : {Id}]. Error: {ErrorMsg}", configToDelete.Name, configToDelete.Id, errorMsg));
    }

    protected override async Task<IEnumerable<UpdatedLeaderboardConfig>> ListConfigsAsync(
        string cloudProjectId,
        string environmentId,
        CancellationToken cancellationToken)
    {
        var configsOnRemote = await m_LeaderboardsService.GetLeaderboardsAsync(
            cloudProjectId,
            environmentId,
            null,
            int.MaxValue,
            cancellationToken
        );

        return configsOnRemote;
    }

    protected override async Task CreateConfigAsync(
        string projectId,
        string environmentId,
        UpdatedLeaderboardConfig config,
        CancellationToken cancellationToken)
    {
        ValidateResponse(await m_LeaderboardsService.CreateLeaderboardAsync(
                projectId,
                environmentId,
                config.ToRequestBody(),
                cancellationToken
            ),
            () => m_Logger.LogInformation("Leaderboard [{ConfigName} : {ConfigId}] successfully created", config.Name, config.Id),
            (errorMsg) => m_Logger.LogError("Failed to create [{ConfigName} : {ConfigId}]. Error: {ErrorMsg}", config.Name, config.Id, errorMsg));
    }

    protected override async Task UpdateConfigAsync(
        string projectId,
        string environmentId,
        UpdatedLeaderboardConfig config,
        CancellationToken cancellationToken)
    {
        ValidateResponse(await m_LeaderboardsService.UpdateLeaderboardAsync(
                projectId,
                environmentId,
                config.Id,
                config.ToRequestBody(),
                cancellationToken),
            () => m_Logger.LogInformation("Leaderboard [{ConfigName} : {ConfigId}] successfully updated", config.Name, config.Id),
            (errorMsg) => m_Logger.LogError("Failed to update [{ConfigName} : {ConfigId}]. Error: {ErrorMsg}", config.Name, config.Id, errorMsg));
    }

    protected override ImportExportEntry<UpdatedLeaderboardConfig> ToImportExportEntry(UpdatedLeaderboardConfig value)
    {
        return new ImportExportEntry<UpdatedLeaderboardConfig>(value.Id.GetHashCode(), $"[{value.Name} : {value.Id}]", value);
    }

    static void ValidateResponse<T>(
        ApiResponse<T> response,
        Action onSuccess,
        Action<string> onError)
    {
        var intStatusCode = (int)response.StatusCode;
        if (intStatusCode is >= 200 and < 300)
        {
            onSuccess();
            return;
        }

        onError(response.ErrorText);
        throw new HttpRequestException(response.ErrorText, null, response.StatusCode);
    }
}
