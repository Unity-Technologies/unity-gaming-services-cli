using Newtonsoft.Json;
using Unity.Services.Cli.Deploy.Model;
using Unity.Services.Cli.RemoteConfig.Deploy;
using Unity.Services.RemoteConfig.Editor.Authoring.Core.Model;

namespace Unity.Services.Cli.RemoteConfig.Deploy;

internal class RemoteConfigScriptsLoader : IRemoteConfigScriptsLoader
{
    const string k_ServiceType = "Remote Config";

    public async Task<IReadOnlyList<RemoteConfigFile>> LoadScriptsAsync(IReadOnlyList<string> filePaths, ICollection<DeployContent> deployContents)
    {
        var remoteConfigFiles = new List<RemoteConfigFile>();
        foreach (var filePath in filePaths)
        {
            var name = Path.GetFileName(filePath);
            try
            {
                var fileText = await File.ReadAllTextAsync(filePath);
                var content = JsonConvert.DeserializeObject<RemoteConfigFileContent>(fileText)!;
                remoteConfigFiles.Add(new RemoteConfigFile(name, filePath, content));
                deployContents.Add(new DeployContent(name, k_ServiceType, filePath, 0, "Loaded"));
            }
            catch (JsonException ex)
            {
                deployContents.Add(new DeployContent(name, k_ServiceType, filePath, 0, "Failed To Read", ex.Message));
            }
        }
        return remoteConfigFiles;
    }
}
