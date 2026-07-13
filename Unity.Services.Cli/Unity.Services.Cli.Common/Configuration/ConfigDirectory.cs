using Unity.Services.Cli.Common.Models;

namespace Unity.Services.Cli.Common;

public static class ConfigDirectory
{
    const string k_DefaultSubDirectory = "UnityServices";

    public static string GetPath()
    {
        var envOverride = Environment.GetEnvironmentVariable(Keys.EnvironmentKeys.ConfigDir);
        if (!string.IsNullOrEmpty(envOverride))
        {
            return envOverride;
        }

        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            k_DefaultSubDirectory);
    }
}
