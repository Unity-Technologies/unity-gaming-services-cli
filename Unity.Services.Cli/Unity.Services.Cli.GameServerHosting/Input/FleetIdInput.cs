using System.CommandLine;
using Unity.Services.Cli.Common.Input;

namespace Unity.Services.Cli.GameServerHosting.Input;

class FleetIdInput : CommonInput
{
    public const string FleetIdKey = "fleet-id";

    public static readonly Argument<string> FleetIdArgument = new(FleetIdKey, "The unique ID of the fleet");

    static FleetIdInput()
    {
        FleetIdArgument.AddValidator(result =>
        {
            var value = result.GetValueOrDefault<string>();
            try
            {
                Guid.Parse(value);
            }
            catch (Exception)
            {
                result.ErrorMessage = $"Fleet '{value}' not a valid UUID.";
            }
        });
    }

    [InputBinding(nameof(FleetIdArgument))]
    public string? FleetId { get; init; }
}
