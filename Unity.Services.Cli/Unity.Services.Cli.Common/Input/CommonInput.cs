using System.CommandLine;
using Unity.Services.Cli.Common.Models;
using Unity.Services.Cli.Common.SystemEnvironment;

namespace Unity.Services.Cli.Common.Input;

/// <summary>
/// Contains all common input that will be shared across most commands.
/// </summary>
public class CommonInput
{
    public static readonly Option<string> EnvironmentNameOption = new(new[]
    {
        "-e",
        "--environment-name"
    }, "The services environment name");

    public static readonly Option<string> CloudProjectIdOption = new(new[]
    {
        "-p",
        "--project-id"
    }, "The Unity cloud project id");

    public static readonly Option<bool> JsonOutputOption = new(new[]
    {
        "-j",
        "--json"
    }, "Use json as the output format")
    {
        Arity = ArgumentArity.Zero,
    };

    public static readonly Option<bool> QuietOption = new(new[]
    {
        "-q",
        "--quiet"
    }, "Reduce logging to a minimum")
    {
        Arity = ArgumentArity.Zero,
    };

    public static readonly Option<bool> UseForceOption = new(new[]
    {
        "-f",
        "--force"
    }, "Force an operation")
    {
        Arity = ArgumentArity.Zero,
    };

    [EnvironmentBinding(Keys.EnvironmentKeys.EnvironmentName)]
    [ConfigBinding(Keys.ConfigKeys.EnvironmentName)]
    [InputBinding(nameof(EnvironmentNameOption))]
    public string? TargetEnvironmentName { get; set; }

    [EnvironmentBinding(Keys.EnvironmentKeys.ProjectId)]
    [ConfigBinding(Keys.ConfigKeys.ProjectId)]
    [InputBinding(nameof(CloudProjectIdOption))]
    public string? CloudProjectId { get; set; }

    [InputBinding(nameof(JsonOutputOption))]
    public bool IsJson { get; set; }

    [InputBinding(nameof(QuietOption))]
    public bool IsQuiet { get; set; }

    [InputBinding(nameof(UseForceOption))]
    public bool UseForce { get; set; }
}
