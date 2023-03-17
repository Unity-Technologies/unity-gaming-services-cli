using Newtonsoft.Json;
using Unity.Services.Cli.Authoring.Model;
using Unity.Services.Cli.RemoteConfig.Deploy;
using Unity.Services.RemoteConfig.Editor.Authoring.Core.Model;

namespace Unity.Services.Cli.RemoteConfig.Deploy;

internal class RemoteConfigScriptsLoader : IRemoteConfigScriptsLoader
{
    const string k_ServiceType = "Remote Config";

    public async Task<LoadResult> LoadScriptsAsync(IReadOnlyList<string> filePaths, ICollection<DeployContent> deployContents)
    {
        var loaded = new List<RemoteConfigFile>();
        var failed = new List<DeployContent>();

        foreach (var filePath in filePaths)
        {
            var name = Path.GetFileName(filePath);
            try
            {
                var fileText = await File.ReadAllTextAsync(filePath);
                var content = JsonConvert.DeserializeObject<RemoteConfigFileContent>(fileText)!;
                loaded.Add(new RemoteConfigFile(name, filePath, content));
                deployContents.Add(new DeployContent(name, k_ServiceType, filePath, 0, "Loaded"));
            }
            catch (JsonException ex)
            {
                var content = new DeployContent(name, k_ServiceType, filePath, 0, "Failed To Read", ex.Message);
                failed.Add(content);
                deployContents.Add(content);
            }
        }
        return new LoadResult(loaded, failed);
    }
}
