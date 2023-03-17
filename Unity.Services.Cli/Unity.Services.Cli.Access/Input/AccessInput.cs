using System.CommandLine;
using Unity.Services.Cli.Common.Input;

namespace Unity.Services.Cli.Access.Input;

public class AccessInput : CommonInput
{
    public static readonly Argument<string> PlayerIdArgument = new(
        name: "player-id", description: "The ID of the player");

    public static readonly Argument<FileInfo> FilePathArgument = new(
        name: "file-path", description: "The path of the JSON file");

    [InputBinding(nameof(PlayerIdArgument))]
    public string? PlayerId { get; set; }

    [InputBinding(nameof(FilePathArgument))]
    public FileInfo? FilePath { get; set; }
}
