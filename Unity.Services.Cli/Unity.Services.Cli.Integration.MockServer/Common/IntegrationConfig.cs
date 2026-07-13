using Newtonsoft.Json;
using Unity.Services.Cli.Common.Validator;

namespace Unity.Services.Cli.MockServer.Common;

public class IntegrationConfig : IDisposable
{
    /// <summary>
    /// Returns the isolated config directory for this fixture instance.
    /// </summary>
    public string ConfigDir { get; }

    /// <summary>
    /// Returns the path to the configuration file used by the config module
    /// </summary>
    public string ConfigurationFile { get; }

    /// <summary>
    /// Returns the path to the configuration file used by the auth module
    /// </summary>
    public string CredentialsFile { get; }

    public IntegrationConfig()
    {
        ConfigDir = Path.Combine(Path.GetTempPath(), "ugs-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(ConfigDir);
        ConfigurationFile = Path.Combine(ConfigDir, "Config.json");
        CredentialsFile = Path.Combine(ConfigDir, "credentials");
    }

    public void SetCredentialValue(string credential)
    {
        File.WriteAllText(CredentialsFile, $"\"{credential}\"");
    }

    public void SetConfigValue(string key, string value)
    {
        var validator = new ConfigurationValidator();
        validator.ThrowExceptionIfConfigInvalid(key, value);

        var config = new Dictionary<string, string>();
        if (File.Exists(ConfigurationFile))
        {
            var content = File.ReadAllText(ConfigurationFile);
            config = JsonConvert.DeserializeObject<Dictionary<string, string>>(content) ?? config;
        }

        config[key] = value;
        File.WriteAllText(ConfigurationFile!, JsonConvert.SerializeObject(config, Formatting.Indented));
    }

    public void Dispose()
    {
        if (Directory.Exists(ConfigDir))
        {
            Directory.Delete(ConfigDir, recursive: true);
        }
    }
}
