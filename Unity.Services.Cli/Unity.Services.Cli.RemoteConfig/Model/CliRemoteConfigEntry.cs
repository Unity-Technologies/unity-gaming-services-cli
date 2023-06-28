using Unity.Services.Cli.Authoring.Model;
using Unity.Services.DeploymentApi.Editor;

namespace Unity.Services.Cli.RemoteConfig.Model;


public class CliRemoteConfigEntry : DeployContent
{
    public CliRemoteConfigEntry(string name, string type, string path, float progress, string status, string? detail = null, SeverityLevel level = SeverityLevel.None)
        : base(name, type, path, progress, status, detail, level)
    { }

    public override string ToString()
    {
        return $"Key '{Name}' in '{Path}'";
    }
}

