using System.IO.Abstractions;
using System.Text;
using Microsoft.Extensions.Logging;
using Unity.Services.Cli.Authoring.Model;
using Unity.Services.Cli.CloudCode.Deploy;
using Unity.Services.Cli.CloudCode.Parameters;
using Unity.Services.CloudCode.Authoring.Editor.Core.Model;
using Unity.Services.DeploymentApi.Editor;

namespace Unity.Services.Cli.CloudCode.Authoring;

class JavaScriptFetchHandler : IJavaScriptFetchHandler
{
    readonly IJavaScriptClient m_Client;
    readonly ICloudCodeScriptParser m_ScriptParser;
    readonly IFile m_File;
    readonly IEqualityComparer<IScript> m_ScriptComparer;

    /*
     * TODO: Instead of using the logger here: refactor FetchResult pattern to support more refined
     * results to contain precise error messages. Like DeployContent in DeploymentResult.
     */
    readonly ILogger m_Logger;

    public JavaScriptFetchHandler(
        IJavaScriptClient client,
        ICloudCodeScriptParser scriptParser,
        IFile file,
        IEqualityComparer<IScript> scriptComparer,
        ILogger logger)
    {
        m_Client = client;
        m_ScriptParser = scriptParser;
        m_File = file;
        m_ScriptComparer = scriptComparer;
        m_Logger = logger;
    }

    public async Task<FetchResult> FetchAsync(
        string rootDirectory,
        IReadOnlyList<IScript> localResources,
        bool dryRun = false,
        bool reconcile = false,
        CancellationToken token = default)
    {
        rootDirectory = IoUtils.NormalizePath(Path.GetFullPath(rootDirectory));

        localResources = localResources.FilterDuplicates(out var duplicateGroups, out var failed);
        if (duplicateGroups.Any())
        {
            var duplicatesMessage = duplicateGroups.GetDuplicatesMessage();
            m_Logger.LogError(duplicatesMessage);
        }

        var remoteScripts = await FetchRemoteScriptsAsync(failed);
        await GetRemoteScriptDetailsAsync(remoteScripts);

        var created = GetCreatedScripts(localResources, remoteScripts, reconcile, rootDirectory);
        var updated = GetUpdatedScripts(localResources, remoteScripts);
        var deleted = GetDeletedScripts(localResources, remoteScripts);

        await EnforceScriptParametersInBodyAsync(created, failed, token);
        await EnforceScriptParametersInBodyAsync(updated, failed, token);

        if (dryRun)
            return CreateResult(dryRun);

        FilterFailedScripts();

        await ApplyFetchAsync(updated, created, deleted, failed, token);

        return CreateResult(dryRun);

        FetchResult CreateResult(bool dryRun)
        {
            FilterFailedScripts();
            var updatedContent = updated.Select(j => GetDeployContent(j, "Updated")).ToList();
            var deletedContent = deleted.Select(j => GetDeployContent(j, "Deleted")).ToList();
            var createdContent = created.Select(j => GetDeployContent(j, "Created")).ToList();
            var fetched = updatedContent
                .Union(deletedContent)
                .Union(createdContent);
            return new FetchResult(
                updatedContent,
                deletedContent,
                createdContent,
                fetched.ToList(),
                failed.Distinct(m_ScriptComparer).Select(j => GetDeployContent(j, "Failed")).ToList(),
                dryRun);
        }

        void FilterFailedScripts()
        {
            deleted = deleted.Except(failed, m_ScriptComparer).ToList();
            updated = updated.Except(failed, m_ScriptComparer).ToList();
            created = created.Except(failed, m_ScriptComparer).ToList();
        }

        DeployContent GetDeployContent(IScript script, string status)
        {
            return new DeployContent(
                script.Name.ToString(),
                "Cloud Code Scripts",
                script.Path,
                100,
                new DeploymentStatus(status, string.Empty));
        }
    }

    internal async Task<List<IScript>> FetchRemoteScriptsAsync(IEnumerable<IScript> failed)
    {
        var rawRemoteScripts = new List<ScriptInfo>();
        rawRemoteScripts.AddRange(await m_Client.ListScripts());

        return rawRemoteScripts
            .Select(r => new CloudCodeScript(r))
            .Except(failed, m_ScriptComparer)
            .ToList();
    }

    internal async Task GetRemoteScriptDetailsAsync(IEnumerable<IScript> scripts)
    {
        foreach (var script in scripts.OfType<CloudCodeScript>())
        {
            var remoteScript = await m_Client.Get(script.Name);
            script.Body = remoteScript.Body;
            script.Parameters = remoteScript.Parameters;
            script.LastPublishedDate = remoteScript.LastPublishedDate;
        }
    }

    internal List<IScript> GetCreatedScripts(
        IEnumerable<IScript> localResources,
        IEnumerable<IScript> remoteScripts,
        bool reconcile,
        string rootDirectory)
    {
        var created = new List<IScript>();
        if (!reconcile)
            return created;

        created = remoteScripts.Except(localResources, m_ScriptComparer)
            .ToList();

        // Make sure to set path for created scripts.
        foreach (var script in created.OfType<CloudCodeScript>())
        {
            script.Path = Path.Combine(rootDirectory, script.Name.ToString());
        }

        return created;
    }

    internal List<IScript> GetUpdatedScripts(IReadOnlyCollection<IScript> localResources, IEnumerable<IScript> remoteScripts)
    {
        var updated = remoteScripts.Intersect(localResources, m_ScriptComparer)
            .ToList();

        // Make sure to set path for updated scripts.
        foreach (var script in updated.OfType<CloudCodeScript>())
        {
            var localScriptToUpdate = localResources.First(x => m_ScriptComparer.Equals(x, script));
            script.Path = localScriptToUpdate.Path;
        }

        return updated;
    }

    internal List<IScript> GetDeletedScripts(IEnumerable<IScript> localResources, IEnumerable<IScript> remoteScripts)
    {
        return localResources.Except(remoteScripts, m_ScriptComparer)
            .ToList();
    }

    internal async Task EnforceScriptParametersInBodyAsync(
        IEnumerable<IScript> scripts, ICollection<IScript> failed, CancellationToken token)
    {
        var builder = new StringBuilder();
        foreach (var script in scripts)
        {
            var (hasParameters, errorMessage) = await m_ScriptParser
                .TryParseScriptParametersAsync(script, token);

            if (hasParameters)
            {
                continue;
            }

            // TODO: LogDebug when Debug is supported.
            if (errorMessage is not null)
            {
                m_Logger.LogWarning(errorMessage);
                failed.Add(script);
                continue;
            }

            // We can't determine parameters for scripts that haven't been published.
            if (!script.Parameters.Any() && string.IsNullOrEmpty(script.LastPublishedDate))
            {
                // TODO: Include this log in the result as a failure.
                m_Logger.LogWarning(
                    $"\"{script.Path}\" parameters couldn't be determined. "
                    + "It isn't published and its body doesn't declare parameters. "
                    + "Please make sure your cloud code script defines its parameters in its body.");
                failed.Add(script);
                continue;
            }

            script.InjectJavaScriptParametersToBody(builder);
        }
    }

    internal async Task ApplyFetchAsync(
        IReadOnlyCollection<IScript> updated,
        IReadOnlyCollection<IScript> created,
        IEnumerable<IScript> deleted,
        List<IScript> failed,
        CancellationToken token)
    {
        var fileOperations = new List<(IScript Script, Task Operation)>(updated.Count + created.Count);
        DeleteLocalFiles();
        CreateOrUpdateLocalFiles(updated);
        CreateOrUpdateLocalFiles(created);

        await WaitForFileOperationsCompletionAsync();

        void DeleteLocalFiles()
        {
            foreach (var script in deleted)
            {
                try
                {
                    m_File.Delete(script.Path);
                }
                catch (Exception)
                {
                    failed.Add(script);
                }
            }
        }

        void CreateOrUpdateLocalFiles(IEnumerable<IScript> scripts)
        {
            fileOperations.AddRange(scripts.Select(StartFileWritingFor));
        }

        (IScript Script, Task Operation) StartFileWritingFor(IScript script)
        {
            var fileCreation = m_File.WriteAllTextAsync(script.Path, script.Body, token);
            return (script, fileCreation);
        }

        async Task WaitForFileOperationsCompletionAsync()
        {
            try
            {
                await Task.WhenAll(fileOperations.Select(x => x.Operation));
            }
            catch (Exception)
            {
                // Exceptions are silenced here because we manually handle all of them right after.
            }

            var failedOperations = fileOperations.Where(x => !x.Operation.IsCompletedSuccessfully)
                .Select(x => x.Script)
                .ToList();
            failed.AddRange(failedOperations);
        }
    }
}
