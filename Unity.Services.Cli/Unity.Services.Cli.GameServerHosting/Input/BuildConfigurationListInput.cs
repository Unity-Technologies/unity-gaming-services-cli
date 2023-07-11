using System.CommandLine;
using System.CommandLine.Parsing;
using Unity.Services.Cli.Common.Input;

namespace Unity.Services.Cli.GameServerHosting.Input;

public class BuildConfigurationListInput : CommonInput
{
    public const string FleetIdKey = "--fleet-id";
    public const string PartialKey = "--partial";

    public static readonly Option<string> FleetIdOption = new(
        FleetIdKey,
        "The fleet ID to filter the build configuration list by."
    );

    public static readonly Option<string> PartialOption = new(
        PartialKey,
        "The partial search to filter the build configuration list by."
    );

    static BuildConfigurationListInput()
    {
        FleetIdOption.AddValidator(ValidateFleetId);
    }

    [InputBinding(nameof(FleetIdOption))]
    public string? FleetId { get; set; }


    [InputBinding(nameof(PartialOption))]
    public string? Partial { get; set; }

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
}
