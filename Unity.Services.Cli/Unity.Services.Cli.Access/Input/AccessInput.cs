using System.CommandLine;
using Unity.Services.Cli.Common.Input;

namespace Unity.Services.Cli.Access.Input;

public class AccessInput : CommonInput
{
    public static readonly Argument<FileInfo> FilePathArgument = new(
        name: "file-path", description: "The path of the JSON file");

    public static readonly Argument<IEnumerable<string>> StatementIdsArgument = new(
        name: "statement-ids", description: "One or more statement IDs to delete")
    {
        Arity = ArgumentArity.OneOrMore
    };

    [InputBinding(nameof(FilePathArgument))]
    public FileInfo? FilePath { get; set; }

    [InputBinding(nameof(StatementIdsArgument))]
    public IEnumerable<string>? StatementIds { get; set; }
}
