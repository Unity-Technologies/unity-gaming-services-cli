using System.CommandLine;
using System.CommandLine.Parsing;
using Unity.Services.Cli.Common.Input;
using Unity.Services.Gateway.GameServerHostingApiV1.Generated.Model;

namespace Unity.Services.Cli.GameServerHosting.Input;

public class ServerListInput : CommonInput
{
    public const string FleetIdKey = "--fleet-id";
    public const string BuildConfigurationIdKey = "--build-configuration-id";
    public const string PartialKey = "--partial";
    public const string StatusKey = "--status";

    public static readonly Option<string> FleetIdOption = new(
        FleetIdKey,
        "The fleet ID to filter the server list by."
    );

    public static readonly Option<string> BuildConfigurationIdOption = new(
        BuildConfigurationIdKey,
        "The build configuration ID to filter the server list by."
    );

    public static readonly Option<string> PartialOption = new(
        PartialKey,
        "The partial search to filter the server list by."
    );

    public static readonly Option<string> StatusOption = new(
        StatusKey,
        "The status to filter the server list by."
    );

    static ServerListInput()
    {
        FleetIdOption.AddValidator(ValidateFleetId);
        BuildConfigurationIdOption.AddValidator(ValidateBuildConfigurationId);
        StatusOption.AddValidator(ValidateStatus);
    }

    [InputBinding(nameof(FleetIdOption))]
    public string? FleetId { get; set; }

    [InputBinding(nameof(BuildConfigurationIdOption))]
    public string? BuildConfigurationId { get; set; }

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

    static void ValidateBuildConfigurationId(OptionResult result)
    {
        var value = result.GetValueOrDefault<string>();
        if (value == null) return;
        try
        {
            _ = long.Parse(value);
        }
        catch (Exception)
        {
            result.ErrorMessage = $"Invalid option for --build-configuration-id. {value} is not a valid number.";
        }
    }

    static void ValidateStatus(OptionResult result)
    {
        var value = result.GetValueOrDefault<string>();
        if (value == null) return;
        try
        {
            Enum.Parse<Server.StatusEnum>(value);
        }
        catch (Exception)
        {
            result.ErrorMessage = $"Invalid option for --status. Did you mean one of the following? {string.Join(", ", Enum.GetNames<Server.StatusEnum>())}";
        }
    }
}
