using System.IO;
using Unity.Services.Cli.Deploy.Model;

namespace Unity.Services.Cli.IntegrationTest;

class RemoteConfigTestCase
{
    public readonly string ConfigValue;
    public readonly string ConfigFileName;
    public readonly string ConfigFilePath;
    public readonly DeployContent DeployedContent;
    public RemoteConfigTestCase(
        string configValue,
        string configFileName,
        float progress,
        string status,
        string detail,
        string directoryPath)
    {
        ConfigValue = configValue;
        ConfigFileName = configFileName;
        ConfigFilePath = Path.Combine(directoryPath, ConfigFileName);
        DeployedContent =
            new DeployContent(ConfigFileName, "Remote Config", ConfigFilePath, progress, status, detail);
    }
}
