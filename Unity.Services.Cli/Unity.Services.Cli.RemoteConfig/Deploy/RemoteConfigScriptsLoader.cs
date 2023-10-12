using System.IO.Abstractions;
using Newtonsoft.Json;
using Unity.Services.Cli.Authoring.Model;
using Unity.Services.Cli.RemoteConfig.Deploy;
using Unity.Services.Cli.RemoteConfig.Exceptions;
using Unity.Services.DeploymentApi.Editor;
using Unity.Services.RemoteConfig.Editor.Authoring.Core.Model;

namespace Unity.Services.Cli.RemoteConfig.Deploy;

class RemoteConfigScriptsLoader : IRemoteConfigScriptsLoader
{
    readonly IFile m_File;
    readonly IPath m_Path;

    public RemoteConfigScriptsLoader(IFile file, IPath path)
    {
        m_File = file;
        m_Path = path;
    }

    public async Task<LoadResult> LoadScriptsAsync(IReadOnlyList<string> filePaths, CancellationToken token)
    {
        var loaded = new List<RemoteConfigFile>();
        var failed = new List<RemoteConfigFile>();

        foreach (var filePath in filePaths)
        {
            var name = m_Path.GetFileName(filePath);
            var file = new RemoteConfigFile(name, filePath);
            try
            {
                var fileText = await m_File.ReadAllTextAsync(filePath, token);
                var content = JsonConvert.DeserializeObject<RemoteConfigFileContent>(fileText);
                if (content is null)
                {
                    throw new JsonException($"{filePath} is not a valid resource");
                }

                content.ToRemoteConfigEntries(file, new RemoteConfigParser(new ConfigTypeDeriver()));

                loaded.Add(file);
                file.Status = new DeploymentStatus(Statuses.Loaded, "");
            }
            catch (Exception ex)
                when (ex is JsonException or ConfigTypeException)
            {
                file.Status = new DeploymentStatus(Statuses.FailedToRead, ex.Message, SeverityLevel.Error);
                failed.Add(file);
            }
        }
        return new LoadResult(loaded, failed);
    }
}
