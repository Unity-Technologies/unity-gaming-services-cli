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

    static bool IsTelemetryDisabled(ISystemEnvironmentProvider systemEnvironmentProvider)
        => !string.IsNullOrEmpty(systemEnvironmentProvider
            .GetSystemEnvironmentVariable(Keys.EnvironmentKeys.TelemetryDisabled, out _));

    /// <summary>
    /// True when the `unity` CLI launched ugs as `unity ugs`.
    /// </summary>
    public static bool IsRunByUnityCli(ISystemEnvironmentProvider systemEnvironmentProvider)
        => !string.IsNullOrEmpty(systemEnvironmentProvider
            .GetSystemEnvironmentVariable(Keys.EnvironmentKeys.UnityCliContextVersion, out _));

    /// <summary>
    /// Whether telemetry (metrics and diagnostics) may be sent. UGS_CLI_TELEMETRY_DISABLED always turns it off.
    /// Under the `unity` CLI it is sent only when UNITY_CLI_CTX_ANALYTICS_ENABLED is 1, which already reflects
    /// the user's analytics choice and DO_NOT_TRACK.
    /// </summary>
    public static bool IsAnalyticsAllowed(ISystemEnvironmentProvider systemEnvironmentProvider)
    {
        if (IsTelemetryDisabled(systemEnvironmentProvider))
        {
            return false;
        }

        if (!IsRunByUnityCli(systemEnvironmentProvider))
        {
            return true;
        }

        // The `unity` CLI sets this to 1 or 0. Anything else, including a missing value, means not allowed.
        return systemEnvironmentProvider
            .GetSystemEnvironmentVariable(Keys.EnvironmentKeys.UnityCliAnalyticsEnabled, out _)?.Trim() == "1";
    }
}
