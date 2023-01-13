using System.IO;
using Unity.Services.Cli.Deploy.Model;

namespace Unity.Services.Cli.IntegrationTest.Deploy;

class DeployTestCase
{
    public readonly string ConfigValue;
    public readonly string ConfigFileName;
    public readonly string ConfigFilePath;
    public readonly DeployContent DeployedContent;
    public DeployTestCase(
        string configValue,
        string configFileName,
        string configType,
        float progress,
        string status,
        string detail,
        string directoryPath)
    {
        ConfigValue = configValue;
        ConfigFileName = configFileName;
        ConfigFilePath = Path.Combine(directoryPath, ConfigFileName);
        DeployedContent =
            new DeployContent(ConfigFileName, configType, ConfigFilePath, progress, status, detail);
    }
}
