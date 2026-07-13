using System.CommandLine;
using Unity.Services.Cli.Common.Input;

namespace Unity.Services.Cli.Triggers.Input;

public class ListTriggersInput : CommonInput
{
    public static readonly Option<int?> LimitOption = new("--limit",
        "The number of triggers to return per page. Defaults to 100");
    [InputBinding(nameof(LimitOption))]
    public int? Limit { get; set; }
}
