using Newtonsoft.Json;
using Unity.Services.Cli.Common.Validator;

namespace Unity.Services.Cli.MockServer.Common;

public class IntegrationConfig : IDisposable
{
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
        var configDir = Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData), "UnityServices");
        Directory.CreateDirectory(configDir);
        ConfigurationFile = Path.Combine(configDir, "Config.json");
        CredentialsFile = Path.Combine(configDir, "credentials");

        BackUpFile(ConfigurationFile);
        BackUpFile(CredentialsFile);
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

    void BackUpFile(string originalPath)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(originalPath)!);

        if (File.Exists(originalPath) && !File.Exists(GetBackUpConfigFile(originalPath)))
        {
            File.Move(originalPath, GetBackUpConfigFile(originalPath), true);
        }
    }

    string GetBackUpConfigFile(string original) => original + $".{GetType()}.back";
    void RestoreFile(string originalPath)
    {
        if (File.Exists(originalPath))
        {
            File.Delete(originalPath);
        }

        var backUpPath = GetBackUpConfigFile(originalPath);
        if (File.Exists(backUpPath))
        {
            File.Move(backUpPath, originalPath, true);
        }
    }

    public void Dispose()
    {
        RestoreFile(ConfigurationFile);
        RestoreFile(CredentialsFile);
    }
}
