using System.CommandLine;
using Unity.Services.Cli.Common.Input;

namespace Unity.Services.Cli.Scheduler.Input;

public class UpdateScheduleStatusInput : CommonInput
{
    public static readonly Argument<string> ScheduleIdArgument = new("schedule-id", "The ID of the schedule.");

    [InputBinding(nameof(ScheduleIdArgument))]
    public string? ScheduleId { get; set; }
}
