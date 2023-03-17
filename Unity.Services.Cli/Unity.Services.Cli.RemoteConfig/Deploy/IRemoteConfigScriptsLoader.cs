using Unity.Services.Cli.Authoring.Model;
using Unity.Services.Cli.RemoteConfig.Deploy;

namespace Unity.Services.Cli.RemoteConfig.Deploy;

interface IRemoteConfigScriptsLoader
{
    Task<LoadResult> LoadScriptsAsync(
        IReadOnlyList<string> filePaths,
        ICollection<DeployContent> deployContents);
}
