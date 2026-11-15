using Unity.Services.Cli.Common.Models;
using Unity.Services.Cli.Common.SystemEnvironment;

namespace Unity.Services.Cli.Common;

/// <summary>
/// Opt-in previews: commands that ship in the public build but are only added when their name is listed in the
/// <c>UGS_CLI_PREVIEW</c> environment variable, for example <c>UGS_CLI_PREVIEW=player-id-providers</c>.
/// </summary>
/// <remarks>
/// Names are separated by commas, semicolons or whitespace and compared case-insensitively. Unknown names are
/// ignored, and a name keeps being accepted once its feature no longer needs opting into, so a script that sets
/// the variable works on older and newer CLIs alike.
/// </remarks>
public static class PreviewFeatures
{
    public const string PlayerIdProviders = "player-id-providers";

    static readonly char[] k_Separators = { ',', ';', ' ', '	' };

    /// <summary>
    /// Whether <paramref name="feature"/> is listed in <c>UGS_CLI_PREVIEW</c>.
    /// </summary>
    public static bool IsEnabled(string feature, ISystemEnvironmentProvider? environment = null)
    {
        var value = (environment ?? new SystemEnvironmentProvider())
            .GetSystemEnvironmentVariable(Keys.EnvironmentKeys.Preview, out _);
        return !string.IsNullOrWhiteSpace(value)
            && value.Split(k_Separators, StringSplitOptions.RemoveEmptyEntries)
                .Contains(feature, StringComparer.OrdinalIgnoreCase);
    }
}
