using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NUnit.Framework;
using Unity.Services.Cli.Common.Validator;

namespace Unity.Services.Cli.IntegrationTest;

/// <summary>
/// A test fixture to facilitate integration testing
/// </summary>
[TestFixture]
public abstract class UgsCliFixture
{
    /// <summary>
    /// Returns the path to the configuration file used by the config module
    /// </summary>
    protected string? ConfigurationFile { get; private set; }

    /// <summary>
    /// Returns the path to the configuration file used by the auth module
    /// </summary>
    protected string? CredentialsFile { get; private set; }

    static Task? s_BuildCliTask;

    [OneTimeSetUp]
    public async Task BuildCliIfNeeded()
    {
        if (File.Exists(UgsCliBuilder.CliPath))
        {
            await TestContext.Progress.WriteLineAsync($"{UgsCliBuilder.CliPath} exists, skipping CLI build...");
            return;
        }

        if (s_BuildCliTask == null)
        {
            await TestContext.Progress.WriteLineAsync("Building UGS CLI...");
            s_BuildCliTask = UgsCliBuilder.Build();
        }

        await s_BuildCliTask;
    }

    [OneTimeSetUp]
    public void SetupLocalConfigFiles()
    {
        var configDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "UnityServices");
        Directory.CreateDirectory(configDir);

        ConfigurationFile = Path.Combine(configDir, "Config.json");
        CredentialsFile = Path.Combine(configDir, "credentials");

        BackUpFile(ConfigurationFile);
        BackUpFile(CredentialsFile);
    }

    [OneTimeTearDown]
    public void RestoreLocalConfigFiles()
    {
        RestoreFile(ConfigurationFile!);
        RestoreFile(CredentialsFile!);
    }

    void BackUpFile(string originalPath)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(originalPath)!);

        if (File.Exists(originalPath))
        {
            File.Move(originalPath, GetBackUpConfigFile(originalPath), true);
        }
    }

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

    public void DeleteLocalConfig()
    {
        if (File.Exists(ConfigurationFile))
        {
            File.Delete(ConfigurationFile);
        }
    }

    public void DeleteLocalCredentials()
    {
        if (File.Exists(CredentialsFile))
        {
            File.Delete(CredentialsFile);
        }
    }

    string GetBackUpConfigFile(string original) => original + $".{GetType()}.back";

    protected static UgsCliTestCase GetLoggedInCli()
    {
        return new UgsCliTestCase()
            .Command($"login --service-key-id {CommonKeys.ValidServiceAccKeyId} --secret-key-stdin")
            .StandardInputWriteLine(CommonKeys.ValidServiceAccSecretKey);
    }
}
