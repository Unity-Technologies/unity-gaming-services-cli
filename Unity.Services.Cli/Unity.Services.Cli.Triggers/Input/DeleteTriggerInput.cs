using System.CommandLine;
using Unity.Services.Cli.Common.Input;

namespace Unity.Services.Cli.Triggers.Input;

public class DeleteTriggerInput : CommonInput
{
    public static readonly Argument<string> TriggerIdArgument = new("trigger-id", "The ID of the trigger to delete.");

    [InputBinding(nameof(TriggerIdArgument))]
    public string? TriggerId { get; set; }
}
