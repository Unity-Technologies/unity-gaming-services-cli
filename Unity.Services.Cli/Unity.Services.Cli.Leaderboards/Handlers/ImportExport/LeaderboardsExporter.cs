using System.IO.Abstractions;
using Microsoft.Extensions.Logging;
using Unity.Services.Cli.Common.Utils;
using Unity.Services.Cli.Authoring.Compression;
using Unity.Services.Cli.Authoring.Export;
using Unity.Services.Cli.Authoring.Model;
using Unity.Services.Cli.Leaderboards.Input;
using Unity.Services.Cli.Leaderboards.Service;
using Unity.Services.Gateway.LeaderboardApiV1.Generated.Model;

namespace Unity.Services.Cli.Leaderboards.Handlers.ImportExport;

class LeaderboardExporter : BaseExporter<UpdatedLeaderboardConfig>
{
    readonly ILeaderboardsService m_LeaderboardsService;

    public LeaderboardExporter(
        ILeaderboardsService leaderboardsService,
        IZipArchiver zipArchiver,
        IUnityEnvironment unityEnvironment,
        IFileSystem fileSystem,
        ILogger logger)
        : base(
        zipArchiver,
        unityEnvironment,
        fileSystem,
        logger)
    {
        m_LeaderboardsService = leaderboardsService;
    }

    protected override string FileName => LeaderboardConstants.ZipName;
    protected override string EntryName => LeaderboardConstants.EntryName;

    public ListLeaderboardInput ListLeaderboardInput { get; set; } = null!;

    protected override async Task<IEnumerable<UpdatedLeaderboardConfig>> ListConfigsAsync(string projectId, string environmentId, CancellationToken cancellationToken)
    {
        return await m_LeaderboardsService.GetLeaderboardsAsync(
            projectId,
            environmentId,
            ListLeaderboardInput.Cursor,
            ListLeaderboardInput.Limit,
            cancellationToken);
    }

    protected override ImportExportEntry<UpdatedLeaderboardConfig> ToImportExportEntry(UpdatedLeaderboardConfig value)
    {
        return new ImportExportEntry<UpdatedLeaderboardConfig>(value.Id.GetHashCode(), $"[{value.Name} : {value.Id}]", value);
    }
}
