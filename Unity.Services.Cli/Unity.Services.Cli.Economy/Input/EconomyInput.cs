using System.CommandLine;
using Unity.Services.Cli.Common.Input;

namespace Unity.Services.Cli.Economy.Input;

public class EconomyInput : CommonInput
{
    public static readonly Argument<string> ResourceIdArgument =
        new("resource-id", "ID of the Economy resource");

    [InputBinding(nameof(ResourceIdArgument))]
    public string? ResourceId { get; set; }

    public static readonly Argument<string> ResourceFilePathArgument =
        new("file-path", "File path of the resource to add");

    [InputBinding(nameof(ResourceFilePathArgument))]
    public string? FilePath { get; set; }

    public static readonly Option<bool> IncludeDraftOption =
        new("--include-draft", "Include draft (unpublished) resources instead of published ones.");

    [InputBinding(nameof(IncludeDraftOption))]
    public bool IncludeDraft { get; set; }
}
