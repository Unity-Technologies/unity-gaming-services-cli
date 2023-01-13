namespace Unity.Services.Cli.Common.Telemetry;

internal static class TagKeys
{
    /// <summary>
    /// Full description of OS being used
    /// </summary>
    public const string OperatingSystem = "operating_system";

    /// <summary>
    /// Shorter description to OS being used: Windows, Linux or MacOS
    /// </summary>
    public const string Platform = "platform";

    public const string ProductName = "product_name";

    /// <summary>
    /// Returns the CI/CD platform if running on it
    /// </summary>
    /// <remarks>
    /// Platforms supported at the moment: Jenkins and Docker
    /// </remarks>
    public const string CicdPlatform = "cicd_platform";

    /// <summary>
    /// Cli version
    /// </summary>
    public const string CliVersion = "application_version";

    public const string DiagnosticName = "name";

    public const string DiagnosticMessage = "message";

    public const string Command = "cli_full_command";
}
