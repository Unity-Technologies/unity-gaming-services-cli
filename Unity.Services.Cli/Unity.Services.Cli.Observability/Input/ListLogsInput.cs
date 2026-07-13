using System.CommandLine;
using Unity.Services.Cli.Common.Input;

namespace Unity.Services.Cli.Observability.Input;

public class ListLogsInput : CommonInput
{
    public static readonly Option<string?> FromOption = new(
        "--from",
        "The starting timestamp of the logs to return. Accepts an RFC3339 timestamp (e.g. 2023-06-29T11:30:22.939Z) or a relative time range (e.g. now-3h).");
    [InputBinding(nameof(FromOption))]
    public string? From { get; set; }

    public static readonly Option<string?> ToOption = new(
        "--to",
        "The ending timestamp of the logs to return. Accepts an RFC3339 timestamp (e.g. 2023-06-29T14:30:22.939Z) or a relative time range (e.g. now).");
    [InputBinding(nameof(ToOption))]
    public string? To { get; set; }

    public static readonly Option<string?> QueryOption = new(
        "--query",
        "A query string used to filter the logs using the logging filter language (e.g. severityText = \"Error\").");
    [InputBinding(nameof(QueryOption))]
    public string? Query { get; set; }

    public static readonly Option<int?> OffsetOption = new(
        "--offset",
        "The offset of the records to return. Used together with --limit for pagination.");
    [InputBinding(nameof(OffsetOption))]
    public int? Offset { get; set; }

    public static readonly Option<int?> LimitOption = new(
        "--limit",
        "The maximum number of records to return (0-100).");
    [InputBinding(nameof(LimitOption))]
    public int? Limit { get; set; }
}
