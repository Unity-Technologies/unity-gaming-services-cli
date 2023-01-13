using System.CommandLine;
using Unity.Services.Cli.Deploy.Input;
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
}
