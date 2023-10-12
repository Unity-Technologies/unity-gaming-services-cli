using System.CommandLine;
using Unity.Services.Cli.Common.Input;

namespace Unity.Services.Cli.Authoring.Input;

/// <summary>
/// Deploy command input
/// </summary>
public class DeployInput : AuthoringInput
{
    public static readonly Argument<ICollection<string>> PathsArgument = new(
        "paths",
        "The paths to deploy from. Accepts multiple directory or file paths. Specify '.' to deploy in current directory");

    [InputBinding(nameof(PathsArgument))]
    public IReadOnlyList<string> Paths { get; set; } = new List<string>();
}
