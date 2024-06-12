using System.CommandLine;
using Unity.Services.Cli.Common.Input;

namespace Unity.Services.Cli.CloudSave.Input;

class ListDataIdsInput : CommonInput
{
    public static readonly Option<string?> StartOption = new Option<string?>("--start", "The custom data ID to start the page from. If not specified, the first page will be returned.");
    [InputBinding(nameof(StartOption))]
    public string? Start { get; set; }

    public static readonly Option<int?> LimitOption = new Option<int?>("--limit", "The maximum number of custom data IDs to return. If not specified, the default limit of 20 will be used.");
    [InputBinding(nameof(LimitOption))]
    public int? Limit { get; set; }
}
