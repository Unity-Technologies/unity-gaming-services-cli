using System.CommandLine;
using Unity.Services.Cli.Common.Input;

namespace Unity.Services.Cli.GameServerHosting.Input;

public class BuildConfigurationCreateInput : CommonInput
{

    public const string BinaryPathKey = "--binary-path";
    public const string BuildIdKey = "--build";
    public const string CommandLineKey = "--command-line";
    public const string ConfigurationKey = "--configuration";
    public const string CoresKey = "--cores";
    public const string MemoryKey = "--memory";
    public const string NameKey = "--name";
    public const string QueryTypeKey = "--query-type";
    public const string SpeedKey = "--speed";
    public const string ReadinessKey = "--readiness";


    public static readonly Option<string> BinaryPathOption = new(BinaryPathKey, "Path to the game binary")
    {
        IsRequired = true
    };

    public static readonly Option<long> BuildIdOption = new(BuildIdKey, "Build to associate with the new build configuration")
    {
        IsRequired = true
    };

    public static readonly Option<string> CommandLineOption = new(CommandLineKey, "Binary launch parameters")
    {
        IsRequired = true
    };

    public static readonly Option<List<string>> ConfigurationOption = new(
        ConfigurationKey,
        "List of \"key:value\" pairs used to configure this build configuration, supports multiple values")
    {
        AllowMultipleArgumentsPerToken = true
    };

    public static readonly Option<long> CoresOption = new(CoresKey, "The number of CPU cores required per server")
    {
        IsRequired = true
    };

    public static readonly Option<long> MemoryOption = new(MemoryKey, "Maximum memory required per server (MB)")
    {
        IsRequired = true
    };

    public static readonly Option<string> NameOption = new(NameKey, "Name to use for the new build configuration")
    {
        IsRequired = true
    };

    public static readonly Option<string> QueryTypeOption = new(QueryTypeKey, "Query type supported by this build configuration")
    {
        IsRequired = true
    };

    public static readonly Option<long> SpeedOption = new(SpeedKey, "CPU utilisation per core")
    {
        IsRequired = true
    };

    public static readonly Option<bool> ReadinessOption = new(ReadinessKey, "Readiness of the build configuration");

    [InputBinding(nameof(BinaryPathOption))]
    public string? BinaryPath { get; init; }

    [InputBinding(nameof(BuildIdOption))]
    public long? BuildId { get; init; }

    [InputBinding(nameof(CommandLineOption))]
    public string? CommandLine { get; init; }

    [InputBinding(nameof(ConfigurationOption))]
    public List<string>? Configuration { get; init; }

    [InputBinding(nameof(CoresOption))]
    public long? Cores { get; init; }

    [InputBinding(nameof(MemoryOption))]
    public long? Memory { get; init; }

    [InputBinding(nameof(NameOption))]
    public string? Name { get; init; }

    [InputBinding(nameof(QueryTypeOption))]
    public string? QueryType { get; init; }

    [InputBinding(nameof(SpeedOption))]
    public long? Speed { get; init; }

    [InputBinding(nameof(ReadinessOption))]
    public bool? Readiness { get; init; }
}
