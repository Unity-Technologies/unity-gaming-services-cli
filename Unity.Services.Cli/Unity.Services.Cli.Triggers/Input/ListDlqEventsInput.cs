using System.CommandLine;
using Unity.Services.Cli.Common.Input;

namespace Unity.Services.Cli.Triggers.Input;

public class ListDlqEventsInput : CommonInput
{
    public static readonly Option<int?> LimitOption = new("--limit",
        "Maximum number of events to return. Defaults to 50.");

    public static readonly Option<string?> StatusOption = new("--status",
        "Filter events by status (pending, replay_queued, processing, resolved).");

    public static readonly Option<DateTime?> CreatedFromOption = new("--created-from",
        "Filter events created on or after this timestamp (RFC3339).");

    public static readonly Option<DateTime?> CreatedToOption = new("--created-to",
        "Filter events created on or before this timestamp (RFC3339).");

    public static readonly Option<string?> ResolutionActionOption = new("--resolution-action",
        "Filter events by resolution action (replayed, discarded).");

    public static readonly Option<Guid?> EventIdOption = new("--event-id",
        "Filter events by original CloudEvent ID.");

    [InputBinding(nameof(LimitOption))]
    public int? Limit { get; set; }

    [InputBinding(nameof(StatusOption))]
    public string? Status { get; set; }

    [InputBinding(nameof(CreatedFromOption))]
    public DateTime? CreatedFrom { get; set; }

    [InputBinding(nameof(CreatedToOption))]
    public DateTime? CreatedTo { get; set; }

    [InputBinding(nameof(ResolutionActionOption))]
    public string? ResolutionAction { get; set; }

    [InputBinding(nameof(EventIdOption))]
    public Guid? EventId { get; set; }
}
