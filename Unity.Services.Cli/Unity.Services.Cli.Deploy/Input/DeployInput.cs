using System.CommandLine;
using Unity.Services.Cli.Common.Input;

namespace Unity.Services.Cli.Deploy.Input;

/// <summary>
/// Deploy command input
/// </summary>
public class DeployInput : CommonInput
{
    public static readonly Argument<ICollection<string>> PathsArgument = new(
        "paths",
        $"The paths to deploy from. Accepts multiple directory or file paths. Specify '.' to deploy in current directory");

    [InputBinding(nameof(PathsArgument))]
    public ICollection<string> Paths { get; set; } = new List<string>();

}
