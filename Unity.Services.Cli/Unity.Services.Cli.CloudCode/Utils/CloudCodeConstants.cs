using Unity.Services.Cli.Common.Telemetry;

namespace Unity.Services.Cli.CloudCode.Utils;

static class CloudCodeConstants
{
    public const string ServiceTypeScripts = "Cloud Code Scripts";
    public const string ServiceTypeModules = "Cloud Code Modules";
    public const string FileExtensionJavaScript = ".js";
    public const string FileExtensionModulesCcm = ".ccm";
    public const string FileExtensionModulesSln = ".sln";
    public const string ZipNameJavaScript = "ugs-cc-scripts.jszip";
    public const string ZipNameModules = "ugs.ccmzip";
    internal static readonly string EntryName = $"__ugs-cli_{TelemetryConfigurationProvider.GetCliVersion()}";

    internal static readonly string EntryNameScripts = "cloud-code scripts";
    internal static readonly string EntryNameModules = "cloud-code modules";

    internal static readonly string ServiceNameScripts = "cloud-code-scripts";
    internal static readonly string ServiceNameModules = "cloud-code-modules";
}
