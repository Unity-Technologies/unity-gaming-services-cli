using System.CommandLine;
using Unity.Services.Cli.Authoring.Input;
using Unity.Services.Cli.Common.Input;

namespace Unity.Services.Cli.CloudCode.Input;

public class CloudCodeInput : DeployInput
{
    public static readonly Argument<string> ScriptNameArgument =
        new("script-name", "Name of the target script");

    public static readonly Argument<string> FilePathArgument =
        new("file-path", "File path of the script to copy");

    public static readonly Option<string> ScriptTypeOption = new(new[]
    {
        "-t",
        "--type"
    }, "Type of the target script");

    public static readonly Option<string> ScriptLanguageOption = new(new[]
    {
        "-l",
        "--language"
    }, "Language of the target script");

    public static readonly Option<int> VersionOption = new(new[]
    {
        "-v",
        "--version"
    }, "The script version to be republished");

    public static readonly Argument<string> ModuleNameArgument =
        new("module-name", "Name of the target module");

    public static readonly Argument<string> ModuleDirectoryArgument =
        new("module-directory", "Directory for the new module");

    public static readonly Option<string?> TargetFrameworkOption =
        new("--target-framework","Target framework for the module (e.g., net8.0); defaults to net9.0 if not specified");

    [InputBinding(nameof(ScriptNameArgument))]
    public string? ScriptName { get; set; }

    [InputBinding(nameof(FilePathArgument))]
    public string? FilePath { get; set; }

    [InputBinding(nameof(ScriptTypeOption))]
    public string? ScriptType { get; set; }

    [InputBinding(nameof(ScriptLanguageOption))]
    public string? ScriptLanguage { get; set; }

    [InputBinding(nameof(VersionOption))]
    public int? Version { get; set; }

    [InputBinding(nameof(ModuleNameArgument))]
    public string? ModuleName { get; set; }

    [InputBinding(nameof(ModuleDirectoryArgument))]
    public string? ModuleDirectory { get; set; }

    [InputBinding(nameof(TargetFrameworkOption))]
    public string? TargetFramework { get; set; }

    static CloudCodeInput()
    {
        var validFrameworks = new[] { "net9.0", "net8.0", "net7.0", "net6.0" };

        TargetFrameworkOption.AddValidator(result =>
        {
            var value = result.GetValueOrDefault<string?>();
            if (value != null && !validFrameworks.Contains(value, StringComparer.OrdinalIgnoreCase))
            {
                result.ErrorMessage = $"Invalid target framework '{value}'. Valid values are: {string.Join(", ", validFrameworks)}";
            }
        });
    }
}
