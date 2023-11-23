using System.CommandLine;
using System.CommandLine.Parsing;
using Newtonsoft.Json;
using Unity.Services.Cli.Common.Input;
using Unity.Services.Gateway.GameServerHostingApiV1.Generated.Model;

namespace Unity.Services.Cli.GameServerHosting.Input;

class FleetUpdateInput : FleetIdInput
{
    public const string NameKey = "--name";
    public const string AllocTtlKey = "--allocation-ttl";
    public const string DeleteTtlKey = "--delete-ttl";
    public const string DisabledDeleteTtlKey = "--disabled-delete-ttl";
    public const string ShutdownTtlKey = "--shutdown-ttl";
    public const string BuildConfigsKey = "--build-configurations";
    public const string UsageSettingsKey = "--usage-setting";

    public static readonly Option<string> FleetNameOption = new(NameKey, "The name of the fleet");

    public static readonly Option<long> AllocTtlOption =
        new(AllocTtlKey, "The allocation TTL for the fleet");

    public static readonly Option<long> DeleteTtlOption =
        new(DeleteTtlKey, "The delete TTL set for the fleet");

    public static readonly Option<long> DisabledDeleteTtlOption =
        new(DisabledDeleteTtlKey, "The disabled delete TTL set for the fleet");

    public static readonly Option<long> ShutdownTtlOption =
        new(ShutdownTtlKey, "The shutdown TTL set for the fleet");

    public static readonly Option<List<long>> BuildConfigsOption = new(
        BuildConfigsKey,
        "A list of build configuration IDs to associate with the fleet")
    {
        AllowMultipleArgumentsPerToken = true
    };

    public static readonly Option<List<string>> UsageSettingsOption = new(
        UsageSettingsKey,
        "A list of usage settings in JSON format to associate with the fleet")
    {
        AllowMultipleArgumentsPerToken = true
    };

    static FleetUpdateInput()
    {
        UsageSettingsOption.AddValidator(ValidateUsageSetting);
    }

    [InputBinding(nameof(FleetNameOption))]
    public string? Name { get; set; }

    [InputBinding(nameof(AllocTtlOption))]
    public long? AllocTtl { get; set; }

    [InputBinding(nameof(DeleteTtlOption))]
    public long? DeleteTtl { get; set; }

    [InputBinding(nameof(DisabledDeleteTtlOption))]
    public long? DisabledDeleteTtl { get; set; }

    [InputBinding(nameof(ShutdownTtlOption))]
    public long? ShutdownTtl { get; set; }

    [InputBinding(nameof(BuildConfigsOption))]
    public List<long>? BuildConfigs { get; set; }

    [InputBinding(nameof(UsageSettingsOption))]
    public List<string>? UsageSettings { get; set; }

    static void ValidateUsageSetting(OptionResult result)
    {
        var values = result.GetValueOrDefault<List<string>>();
        foreach (var setting in values!)
        {
            try
            {
                JsonConvert.DeserializeObject<FleetUsageSetting>(setting);
            }
            catch (Exception)
            {
                result.ErrorMessage = $"Invalid option for --usage-setting. '{setting}' is not a valid JSON usage setting object.";
            }
        }
    }
}
