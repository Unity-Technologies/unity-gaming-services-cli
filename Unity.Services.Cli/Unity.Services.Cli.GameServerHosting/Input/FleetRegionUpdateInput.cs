using System.CommandLine;
using Unity.Services.Cli.Common.Input;

namespace Unity.Services.Cli.GameServerHosting.Input;

public class FleetRegionUpdateInput : CommonInput
{
    public const string FleetIdKey = "--fleet-id";
    public const string RegionIdKey = "--region-id";
    public const string DeleteTtlKey = "--delete-ttl";
    public const string DisabledDeleteTtlKey = "--disabled-delete-ttl";
    public const string MaxServersKey = "--max-servers";
    public const string MinAvailableServersKey = "--min-available-servers";
    public const string ScalingEnabledKey = "--scaling-enabled";
    public const string ShutdownTtlKey = "--shutdown-ttl";

    public static readonly Option<Guid> FleetIdOption = new(
        FleetIdKey,
        "The ID of the Fleet to update the Fleet Region for."
    )
    {
        IsRequired = true
    };

    public static readonly Option<Guid> RegionIdOption = new(
        RegionIdKey,
        "The ID of the Region to update the Fleet Region for."
    )
    {
        IsRequired = true
    };

    public static readonly Option<long> DeleteTtlOption = new(
        DeleteTtlKey,
        "The delete TTL set for the fleet"
    )
    {
        IsRequired = true
    };

    public static readonly Option<long> DisabledDeleteTtlOption = new(
        DisabledDeleteTtlKey,
        "The disabled delete TTL set for the fleet."
    )
    {
        IsRequired = true
    };

    public static readonly Option<long> MaxServersOption = new(
        MaxServersKey,
        "The maximum number of servers to host in the fleet region."
    )
    {
        IsRequired = true
    };

    public static readonly Option<long> MinAvailableServersOption = new(
        MinAvailableServersKey,
        "The minimum number of servers to keep free for new game sessions."
    )
    {
        IsRequired = true
    };

    public static readonly Option<bool> ScalingEnabledOption = new(
        ScalingEnabledKey,
        "Whether scaling should be enabled for the fleet."
    )
    {
        IsRequired = true
    };

    public static readonly Option<long> ShutdownTtlOption = new(
        ShutdownTtlKey,
        "The shutdown TTL set for the fleet."
    )
    {
        IsRequired = true
    };

    [InputBinding(nameof(FleetIdOption))]
    public Guid? FleetId { get; set; }

    [InputBinding(nameof(RegionIdOption))]
    public Guid? RegionId { get; set; }

    [InputBinding(nameof(DeleteTtlOption))]
    public long? DeleteTtl { get; set; }

    [InputBinding(nameof(DisabledDeleteTtlOption))]
    public long? DisabledDeleteTtl { get; set; }

    [InputBinding(nameof(MaxServersOption))]
    public long? MaxServers { get; set; }

    [InputBinding(nameof(MinAvailableServersOption))]
    public long? MinAvailableServers { get; set; }

    [InputBinding(nameof(ScalingEnabledOption))]
    public bool? ScalingEnabled { get; set; }

    [InputBinding(nameof(ShutdownTtlOption))]
    public long? ShutdownTtl { get; set; }
}
