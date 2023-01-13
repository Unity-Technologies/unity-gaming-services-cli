using Unity.Services.Cli.Deploy.Model;
using Unity.Services.Cli.RemoteConfig.Deploy;

namespace Unity.Services.Cli.RemoteConfig.Deploy;

interface IRemoteConfigScriptsLoader
{
    Task<IReadOnlyList<RemoteConfigFile>> LoadScriptsAsync(
        IReadOnlyList<string> filePaths,
        ICollection<DeployContent> deployContents);
}
