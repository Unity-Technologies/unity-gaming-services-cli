using System.CommandLine;
using Unity.Services.Cli.Common.Input;

namespace Unity.Services.Cli.Authoring.Input;

/// <summary>
/// Fetch command input
/// </summary>
public class FetchInput : AuthoringInput
{
    public static readonly Argument<string> PathArgument = new(
        "path",
        $"The path to fetch to. Accepts a directory path to fetch to. Specify '.' to fetch in current directory");

    [InputBinding(nameof(PathArgument))]
    public string Path { get; set; } = string.Empty;
}
