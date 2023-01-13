using System.CommandLine;
using Unity.Services.Cli.Common.Input;

namespace Unity.Services.Cli.Environment.Input;

public class EnvironmentInput : CommonInput
{
    public static readonly Argument<string> EnvironmentNameArgument = new("environment-name", "The services environment name");

    [InputBinding(nameof(EnvironmentNameArgument))]
    public string? EnvironmentName { get; set; }
}
