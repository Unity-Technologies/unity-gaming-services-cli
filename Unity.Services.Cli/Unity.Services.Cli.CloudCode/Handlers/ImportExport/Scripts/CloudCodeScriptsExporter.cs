using System.IO.Abstractions;
using Microsoft.Extensions.Logging;
using Unity.Services.Cli.Authoring.Compression;
using Unity.Services.Cli.Authoring.Export;
using Unity.Services.Cli.Authoring.Model;
using Unity.Services.Cli.CloudCode.Deploy;
using Unity.Services.Cli.CloudCode.Service;
using Unity.Services.Cli.CloudCode.Utils;
using Unity.Services.Cli.Common.Utils;

namespace Unity.Services.Cli.CloudCode.Handlers.ImportExport.Scripts;

class CloudCodeScriptsExporter : BaseExporter<CloudCodeScript>
{
    readonly ICloudCodeService m_CloudCodeService;

    public CloudCodeScriptsExporter(
        ICloudCodeService cloudCodeService,
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
        m_CloudCodeService = cloudCodeService;
    }

    protected override string FileName => CloudCodeConstants.ZipNameJavaScript;
    protected override string EntryName => CloudCodeConstants.EntryNameScripts;
    protected override async Task<IEnumerable<CloudCodeScript>> ListConfigsAsync(string projectId, string environmentId, CancellationToken cancellationToken)
    {
        var scriptResults = await m_CloudCodeService.ListAsync(
            projectId, environmentId, cancellationToken);

        var scriptNames = scriptResults.Select(s => s.Name);
        var scriptsWithData = await ImportExportUtils.GetScriptDetails(projectId, environmentId, scriptNames, m_CloudCodeService, cancellationToken);
        var scriptsWithDataArray = scriptsWithData as CloudCodeScript[] ?? scriptsWithData.ToArray();

        return scriptsWithDataArray;
    }

    protected override ImportExportEntry<CloudCodeScript> ToImportExportEntry(CloudCodeScript value)
    {
        return new ImportExportEntry<CloudCodeScript>(value.Name.GetHashCode(), value.Name.ToString(), value);
    }
}
