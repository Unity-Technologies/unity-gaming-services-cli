using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using Unity.Services.Cli.Authoring.Compression;
using Unity.Services.Cli.Authoring.Import;
using Unity.Services.Cli.Authoring.Model;
using Unity.Services.Cli.CloudCode.Deploy;
using Unity.Services.Cli.CloudCode.Service;
using Unity.Services.Cli.CloudCode.Utils;
using Unity.Services.Cli.Common.Exceptions;
using Unity.Services.Cli.Common.Utils;
using Unity.Services.Gateway.CloudCodeApiV1.Generated.Client;
using Unity.Services.Gateway.CloudCodeApiV1.Generated.Model;
using Language = Unity.Services.Gateway.CloudCodeApiV1.Generated.Model.Language;

namespace Unity.Services.Cli.CloudCode.Handlers.ImportExport.Scripts;

class CloudCodeScriptsImporter : BaseImporter<CloudCodeScript>
{
    internal const long ScriptAlreadyActive = 9018;
    readonly ICloudCodeService m_CloudCodeService;

    public CloudCodeScriptsImporter(
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

    protected override string FileName => CloudCodeConstants.ZipNameJavaScript;
    protected override string EntryName => CloudCodeConstants.EntryNameScripts;

    protected override async Task DeleteConfigAsync(
        string projectId,
        string environmentId,
        CloudCodeScript script,
        CancellationToken cancellationToken)
    {
        try
        {
            await m_CloudCodeService.DeleteAsync(
                projectId,
                environmentId,
                script.Name.ToString(),
                cancellationToken);
        }
        catch (ApiException e)
        {
            throw new CliException($"Failed to delete [{script.Name}]. ", e, ExitCode.HandledError);
        }

        m_Logger.LogInformation("{Name} successfully deleted", script.Name);
    }

    protected override async Task<IEnumerable<CloudCodeScript>> ListConfigsAsync(
        string cloudProjectId,
        string environmentId,
        CancellationToken cancellationToken)
    {
        var scriptsOnRemote = await m_CloudCodeService.ListAsync(
            cloudProjectId,
            environmentId,
            cancellationToken
        );

        var scriptNames = scriptsOnRemote.Select(s => s.Name);
        var scriptsWithData = await ImportExportUtils.GetScriptDetails(cloudProjectId, environmentId, scriptNames, m_CloudCodeService, cancellationToken);
        var scriptsWithDataArray = scriptsWithData as CloudCodeScript[] ?? scriptsWithData.ToArray();
        return scriptsWithDataArray;
    }

    protected override async Task CreateConfigAsync(string projectId, string environmentId, CloudCodeScript script, CancellationToken cancellationToken)
    {
        try
        {
            var parameters = ImportExportUtils.ConvertAuthoringParamsToParams(script.Parameters);
            await m_CloudCodeService.CreateAsync(
                projectId,
                environmentId,
                script.Name.ToString(),
                ScriptType.API,
                Language.JS,
                script.Body,
                parameters,
                cancellationToken);
            await m_CloudCodeService.PublishAsync(projectId, environmentId, script.Name.ToString(), 0,
                cancellationToken);
        }
        catch (ApiException e)
        {
            var supress = ShouldSuppressException(e);
            if (!supress)
                throw new CliException($"Failed to create [{script.Name}]. ", e, ExitCode.HandledError);
        }

        m_Logger.LogInformation("Script [{ConfigName}] successfully created", script.Name);
    }

    protected override async Task UpdateConfigAsync(string projectId, string environmentId, CloudCodeScript script, CancellationToken cancellationToken)
    {
        try
        {
            var parameters = ImportExportUtils.ConvertAuthoringParamsToParams(script.Parameters);
            await m_CloudCodeService.UpdateAsync(
                projectId,
                environmentId,
                script.Name.ToString(),
                script.Body,
                parameters,
                cancellationToken);
            await m_CloudCodeService.PublishAsync(projectId, environmentId, script.Name.ToString(), 0,
                cancellationToken);
        }
        catch (ApiException e)
        {
            var supress = ShouldSuppressException(e);
            if (!supress)
                throw new CliException($"Failed to update [{script.Name}]. ", e, ExitCode.HandledError);
        }

        m_Logger.LogInformation("Script [{ConfigName}] successfully updated", script.Name);
    }

    static bool ShouldSuppressException(ApiException e)
    {
        var wasAlreadyPublished = false;
        if (e.ErrorContent is string errContent)
        {
            var errDetails = JsonConvert.DeserializeObject<JObject>(errContent)!;
            var code = errDetails["code"]?.Value<long>() ?? 0;
            if (code == ScriptAlreadyActive)
            {
                wasAlreadyPublished = true;
            }
        }

        return wasAlreadyPublished;
    }

    protected override ImportExportEntry<CloudCodeScript> ToImportExportEntry(CloudCodeScript value)
    {
        return new ImportExportEntry<CloudCodeScript>(value.Name.GetHashCode(), value.Name.ToString(), value);
    }
}
