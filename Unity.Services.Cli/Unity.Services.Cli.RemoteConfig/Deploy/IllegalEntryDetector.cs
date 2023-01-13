using Unity.Services.RemoteConfig.Editor.Authoring.Core.ErrorHandling;
using Unity.Services.RemoteConfig.Editor.Authoring.Core.Formatting;
using Unity.Services.RemoteConfig.Editor.Authoring.Core.Model;

namespace Unity.Services.Cli.RemoteConfig.Deploy;

class IllegalEntryDetector : IIllegalEntryDetector
{
    public bool ContainsIllegalEntries(IRemoteConfigFile remoteConfigFile, ICollection<RemoteConfigDeploymentException> deploymentExceptions)
    {
        return false;
    }
}
