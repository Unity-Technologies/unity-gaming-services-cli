using System.CommandLine;
using System.CommandLine.Parsing;
using Unity.Services.Cli.Common.Input;
using Machine = Unity.Services.Gateway.GameServerHostingApiV1.Generated.Model.Machine1;

namespace Unity.Services.Cli.GameServerHosting.Input;

public class MachineListInput : CommonInput
{
    public const string FleetIdKey = "--fleet-id";
    public const string HardwareTypeKey = "--hardware-type";
    public const string LocationIdKey = "--location-id";
    public const string PartialKey = "--partial";
    public const string StatusKey = "--status";

    public static readonly Option<string> FleetIdOption = new(
        FleetIdKey,
        "The fleet ID to filter the machine list by."
    );

    public static readonly Option<string> HardwareTypeOption = new(
        HardwareTypeKey,
        "The hardware type to filter the machine list by."
    );

    public static readonly Option<string> LocationIdOption = new(
        LocationIdKey,
        "The location ID to filter the machine list by."
    );

    public static readonly Option<string> PartialOption = new(
        PartialKey,
        "The partial search to filter the machine list by."
    );

    public static readonly Option<string> StatusOption = new(
        StatusKey,
        "The status to filter the machine list by."
    );

    static MachineListInput()
    {
        FleetIdOption.AddValidator(ValidateFleetId);
        HardwareTypeOption.AddValidator(ValidateHardwareType);
        StatusOption.AddValidator(ValidateStatus);
    }

    [InputBinding(nameof(FleetIdOption))]
    public string? FleetId { get; set; }

    [InputBinding(nameof(HardwareTypeOption))]
    public string? HardwareType { get; set; }

    [InputBinding(nameof(LocationIdOption))]
    public string? LocationId { get; set; }

    [InputBinding(nameof(PartialOption))]
    public string? Partial { get; set; }

    [InputBinding(nameof(StatusOption))]
    public string? Status { get; set; }

    static void ValidateFleetId(OptionResult result)
    {
        var value = result.GetValueOrDefault<string>();
        if (value == null) return;
        try
        {
            Guid.Parse(value);
        }
        catch (Exception)
        {
            result.ErrorMessage = $"Invalid option for --fleet-id. {value} is not a valid UUID";
        }
    }

    static void ValidateHardwareType(OptionResult result)
    {
        var value = result.GetValueOrDefault<string>();
        if (value == null) return;
        try
        {
            Enum.Parse<Machine.HardwareTypeEnum>(value);
        }
        catch (Exception)
        {
            result.ErrorMessage = $"Invalid option for --hardware-type. Did you mean one of the following? {string.Join(", ", Enum.GetNames<Machine.HardwareTypeEnum>())}";
        }
    }

    static void ValidateStatus(OptionResult result)
    {
        var value = result.GetValueOrDefault<string>();
        if (value == null) return;
        try
        {
            Enum.Parse<Machine.StatusEnum>(value);
        }
        catch (Exception)
        {
            result.ErrorMessage = $"Invalid option for --status. Did you mean one of the following? {string.Join(", ", Enum.GetNames<Machine.StatusEnum>())}";
        }
    }
}
