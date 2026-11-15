using System.CommandLine;
using Unity.Services.Cli.Common.Input;

namespace Unity.Services.Cli.Triggers.Input;

public class UpdateTriggerStatusInput : CommonInput
{
    public static readonly Argument<string> TriggerIdArgument = new("trigger-id", "The ID of the trigger.");

    [InputBinding(nameof(TriggerIdArgument))]
    public string? TriggerId { get; set; }
}
