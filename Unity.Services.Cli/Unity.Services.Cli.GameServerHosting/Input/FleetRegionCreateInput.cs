using System.CommandLine;
using Unity.Services.Cli.Common.Input;

namespace Unity.Services.Cli.GameServerHosting.Input;

public class FleetRegionCreateInput : CommonInput
{
    public const string FleetIdKey = "--fleet-id";
    public const string RegionIdKey = "--region-id";
    public const string MaxServersKey = "--max-servers";
    public const string MinAvailableServersKey = "--min-available-servers";

    public static readonly Option<Guid> FleetIdOption = new(
        FleetIdKey,
        "The ID of the Fleet to create the Fleet Region in."
    )
    {
        IsRequired = true
    };

    public static readonly Option<Guid> RegionIdOption = new(
        RegionIdKey,
        "The region ID to be added to the fleet. See the fleet region available command for a list of available regions."
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

    [InputBinding(nameof(FleetIdOption))]
    public Guid? FleetId { get; set; }

    [InputBinding(nameof(RegionIdOption))]
    public Guid? RegionId { get; set; }

    [InputBinding(nameof(MaxServersOption))]
    public long? MaxServers { get; set; }

    [InputBinding(nameof(MinAvailableServersOption))]
    public long? MinAvailableServers { get; set; }

}
