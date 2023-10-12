using System.IO.Abstractions;
using System.IO.Compression;
using Microsoft.Extensions.Logging;
using Unity.Services.Cli.Authoring.Compression;
using Unity.Services.Cli.Authoring.Export;
using Unity.Services.Cli.Authoring.Model;
using Unity.Services.Cli.CloudCode.Deploy;
using Unity.Services.Cli.CloudCode.Service;
using Unity.Services.Cli.CloudCode.Utils;
using Unity.Services.Cli.Common.Utils;
using Unity.Services.CloudCode.Authoring.Editor.Core.Model;
using Module = Unity.Services.Cli.CloudCode.Deploy.CloudCodeModule;

namespace Unity.Services.Cli.CloudCode.Handlers.ImportExport.Modules;

class CloudCodeModulesExporter : BaseExporter<Module>
{
    readonly ICloudCodeService m_CloudCodeService;
    readonly ICloudCodeModulesDownloader m_ModulesDownloader;

    public CloudCodeModulesExporter(
        ICloudCodeService cloudCodeService,
        ICloudCodeModulesDownloader cloudCodeModulesDownloader,
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
        m_ModulesDownloader = cloudCodeModulesDownloader;
    }

    protected override string FileName => CloudCodeConstants.ZipNameModules;
    protected override string EntryName => CloudCodeConstants.EntryNameModules;

    protected override async Task<IEnumerable<Module>> ListConfigsAsync(string projectId, string environmentId, CancellationToken cancellationToken)
    {
        var moduleResults = await m_CloudCodeService.ListModulesAsync(
            projectId, environmentId, cancellationToken);

        var modules = moduleResults.Select(r =>
            new Module(new ScriptName(r.Name), Language.JS, $"{r.Name}{CloudCodeConstants.FileExtensionModulesCcm}", r.SignedDownloadURL));

        return modules;
    }

    protected override ImportExportEntry<Module> ToImportExportEntry(Module value)
    {
        return new ImportExportEntry<Module>(value.Name.GetHashCode(), value.Name.ToString(), value);
    }

    protected override async Task AfterZip(IEnumerable<Module> configs, string archivePath, CancellationToken cancellationToken)
    {
        using var zipArchive = ZipFile.Open(archivePath, ZipArchiveMode.Update);

        foreach (var module in configs)
        {
            var entry = zipArchive.CreateEntry(module.Name.ToString());
            var stream = entry.Open();
            var stream2 = await m_ModulesDownloader.DownloadModule(module, cancellationToken);
            await stream2.CopyToAsync(stream, cancellationToken);
        }
    }
}
