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

    // No "-v" alias: that belongs to VersionOption above, which is an int owned by scripts publish.
    // Wording is deliberately generic: this option is registered on both `modules get`, where the
    // versions are retained generations, and `scripts get`, where they are published versions plus
    // the working copy. Each command's own description says which.
    public static readonly Option<bool> VersionsOption = new(
        "--versions",
        "List versions as well as the current state");

    // A separate option from VersionOption, which is an int for republishing a script. This one is a
    // selector, so it must stay a string; it shares the --version name only because the two are
    // never registered on the same command.
    public static readonly Option<string?> ModuleSpecVersionOption = new(
        "--version",
        "Which version of the module to describe: 'latest', or a version number as listed by 'modules get --versions'. Defaults to the live version.");

    public static readonly Argument<string> ModuleNameArgument =
        new("module-name", "Name of the target module");

    public static readonly Argument<string> ModuleDirectoryArgument =
        new("module-directory", "Directory for the new module");

    // Typed as a number so an indirect selector such as 'latest' is rejected at parse time: a delete
    // never resolves a selector on the caller's behalf.
    public static readonly Argument<long> ModuleVersionArgument =
        new("version", "The module version to delete, as listed by 'modules get --versions'");

    public static readonly Argument<int> ScriptVersionArgument =
        new("version", "The script version to delete, as listed by 'scripts get --versions'");

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

    [InputBinding(nameof(VersionsOption))]
    public bool Versions { get; set; }

    [InputBinding(nameof(ModuleSpecVersionOption))]
    public string? ModuleSpecVersion { get; set; }

    [InputBinding(nameof(ModuleVersionArgument))]
    public long ModuleVersion { get; set; }

    [InputBinding(nameof(ScriptVersionArgument))]
    public int ScriptVersion { get; set; }

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
