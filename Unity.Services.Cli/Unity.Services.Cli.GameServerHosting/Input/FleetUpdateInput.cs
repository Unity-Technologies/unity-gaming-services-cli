using System.CommandLine;
using Unity.Services.Cli.Common.Input;

namespace Unity.Services.Cli.GameServerHosting.Input;

class FleetUpdateInput : FleetIdInput
{
    public const string NameKey = "--name";
    public const string AllocTtlKey = "--allocation-ttl";
    public const string DeleteTtlKey = "--delete-ttl";
    public const string DisabledDeleteTtlKey = "--disabled-delete-ttl";
    public const string ShutdownTtlKey = "--shutdown-ttl";
    public const string BuildConfigsKey = "--build-configurations";

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
}
