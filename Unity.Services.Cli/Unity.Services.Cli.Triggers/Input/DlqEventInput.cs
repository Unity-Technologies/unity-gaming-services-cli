using System.CommandLine;
using Unity.Services.Cli.Common.Input;

namespace Unity.Services.Cli.Triggers.Input;

public class DlqEventInput : CommonInput
{
    public static readonly Argument<string> EventIdArgument = new("event-id", "The ID of the DLQ event.");

    [InputBinding(nameof(EventIdArgument))]
    public string? EventId { get; set; }
}
