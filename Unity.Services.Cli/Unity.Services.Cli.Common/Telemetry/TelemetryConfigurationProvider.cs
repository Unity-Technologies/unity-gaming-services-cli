using System.Runtime.InteropServices;
using Unity.Services.Cli.Common.Models;
using Unity.Services.Cli.Common.Networking;
using Unity.Services.Cli.Common.SystemEnvironment;

namespace Unity.Services.Cli.Common.Telemetry;

public static class TelemetryConfigurationProvider
{
    public static string GetOsPlatform()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            return "Linux";
        }
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return "Windows";
        }
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return "MacOS";
        }
        return "Unknown";
    }

    public static string GetCicdPlatform(ISystemEnvironmentProvider systemEnvironmentProvider)
    {
        foreach (var cicdEnvKey in Keys.EnvironmentKeys.cicdKeys)
        {
            if (!string.IsNullOrEmpty(systemEnvironmentProvider.GetSystemEnvironmentVariable(cicdEnvKey, out _)))
            {
                return Keys.CicdEnvVarToDisplayNamePair[cicdEnvKey];
            }
        }

        return "";
    }

    public static string GetCliVersion()
        => RequestHeaderHelper.XClientIdHeaderValue.Split('@').Last();

    public static bool IsTelemetryDisabled(ISystemEnvironmentProvider systemEnvironmentProvider)
        => !string.IsNullOrEmpty(systemEnvironmentProvider
            .GetSystemEnvironmentVariable(Keys.EnvironmentKeys.TelemetryDisabled, out _));
}
