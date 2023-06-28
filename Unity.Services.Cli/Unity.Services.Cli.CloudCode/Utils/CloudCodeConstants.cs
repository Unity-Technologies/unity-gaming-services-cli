using Unity.Services.Cli.Common.Telemetry;

namespace Unity.Services.Cli.CloudCode.Utils;

static class CloudCodeConstants
{
    public const string ServiceType = "Cloud Code Scripts";
    public const string ServiceTypeModules = "Cloud Code Modules";
    public const string JavaScriptFileExtension = ".js";
    public const string JavascriptZipName = "ugs-cc-scripts.jszip";
    public const string SingleModuleFileExtension = ".ccm";
    public const string ModulesZipName = "ugs.ccmzip";
    internal static readonly string EntryName = $"__ugs-cli_{TelemetryConfigurationProvider.GetCliVersion()}";

    internal static readonly string ModulesEntryName = "cloud-code modules";
    internal static readonly string ScriptsEntryName = "cloud-code scripts";

    internal static readonly string ServiceName = "cloud-code-scripts";
    internal static readonly string ServiceNameModule = "cloud-code-modules";
}
