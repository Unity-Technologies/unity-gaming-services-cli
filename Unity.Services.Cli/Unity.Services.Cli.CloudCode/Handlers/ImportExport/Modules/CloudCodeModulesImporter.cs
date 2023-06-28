using System.IO.Compression;
using Microsoft.Extensions.Logging;
using Unity.Services.Cli.Authoring.Compression;
using Unity.Services.Cli.Authoring.Import;
using Unity.Services.Cli.Authoring.Model;
using Unity.Services.Cli.CloudCode.Service;
using Unity.Services.Cli.CloudCode.Utils;
using Unity.Services.Cli.Common.Exceptions;
using Unity.Services.Cli.Common.Utils;
using Unity.Services.CloudCode.Authoring.Editor.Core.Model;
using Unity.Services.Gateway.CloudCodeApiV1.Generated.Client;
using Module = Unity.Services.Cli.CloudCode.Deploy.CloudCodeModule;
namespace Unity.Services.Cli.CloudCode.Handlers.ImportExport.Modules;

class CloudCodeModulesImporter : BaseImporter<Module>
{
    readonly ICloudCodeService m_CloudCodeService;

    public CloudCodeModulesImporter(
        ICloudCodeService cloudCodeService,
        IZipArchiver zipArchiver,
        IUnityEnvironment unityEnvironment,
        ILogger logger)
        : base(
            zipArchiver,
            unityEnvironment,
            logger)
    {
        m_CloudCodeService = cloudCodeService;
    }

    protected override string EntryName => CloudCodeConstants.ModulesEntryName;
    protected override string FileName => CloudCodeConstants.ModulesZipName;

    protected override async Task DeleteConfigAsync(
        string projectId,
        string environmentId,
        Module moduleToDelete,
        CancellationToken cancellationToken)
    {
        try
        {
            await m_CloudCodeService.DeleteModuleAsync(
                projectId,
                environmentId,
                moduleToDelete.Name.ToString(),
                cancellationToken);
        }
        catch (ApiException e)
        {
            m_Logger.LogError("Failed to delete {Name}. Error: {ErrorMsg}", moduleToDelete.Name, e.ErrorContent);
        }


        m_Logger.LogInformation("{Name} successfully deleted", moduleToDelete.Name);
    }

    protected override async Task<IEnumerable<Module>> ListConfigsAsync(
        string cloudProjectId,
        string environmentId,
        CancellationToken cancellationToken)
    {
        var moduleResults = await m_CloudCodeService.ListModulesAsync(
            cloudProjectId, environmentId, cancellationToken);

        var modules = moduleResults.Select(
            r => new Module(new ScriptName(r.Name), Language.JS, $"{r.Name}{CloudCodeConstants.SingleModuleFileExtension}", r.SignedDownloadURL));

        return modules;
    }

    protected override async Task CreateConfigAsync(string projectId, string environmentId, Module module, CancellationToken cancellationToken)
    {
        try
        {
            await CreateOrUpdateModule(projectId, environmentId, module, cancellationToken);
        }
        catch (ApiException e)
        {
            m_Logger.LogError("Failed to create [{ConfigName}]. Error: {ErrorMsg}", module.Name, e.ErrorContent);
            throw;
        }

        m_Logger.LogInformation("Module [{ConfigName}] successfully created", module.Name);
    }

    protected override async Task UpdateConfigAsync(string projectId, string environmentId, Module module, CancellationToken cancellationToken)
    {
        try
        {
            await CreateOrUpdateModule(projectId, environmentId, module, cancellationToken);
        }
        catch (ApiException e)
        {
            m_Logger.LogError("Failed to update [{ConfigName}]. Error: {ErrorMsg}", module.Name, e.ErrorContent);
            throw;
        }

        m_Logger.LogInformation("Script [{ConfigName}] successfully updated", module.Name);
    }

    protected override ImportExportEntry<Module> ToImportExportEntry(Module value)
    {
        return new ImportExportEntry<Module>(value.Name.GetHashCode(), value.Name.ToString(), value);
    }

    async Task CreateOrUpdateModule(
        string projectId,
        string environmentId,
        Module module,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(module.Path))
        {
            throw new CliException("The module path provided is null or empty.", ExitCode.HandledError);
        }

        using var entry = m_ZipArchiver.GetEntry(m_ArchivePath!, module.Name.ToString());

        if (entry == null)
        {
            throw new CliException($"Expected entry '{module.Name}' was not found in zip file.", ExitCode.UnhandledError);
        }

        using var stream = new MemoryStream();
        await entry.Stream.CopyToAsync(stream, cancellationToken);
        //after finishing the copy, reset the head of the stream so it can be sent
        stream.Position = 0;
        await m_CloudCodeService.UpdateModuleAsync(
            projectId,
            environmentId,
            module.Name.ToString(),
            stream,
            cancellationToken);
    }
}
