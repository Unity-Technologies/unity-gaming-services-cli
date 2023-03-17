using System.CommandLine;
using Unity.Services.Cli.Common.Input;

namespace Unity.Services.Cli.Authoring.Input;

public class NewFileInput : CommonInput
{
    public static readonly Argument<string> FileArgument = new(
        "file name",
        () => "new_config",
        "The name of the file to create");

    [InputBinding(nameof(FileArgument))]
    public string? File { get; set; }
}
