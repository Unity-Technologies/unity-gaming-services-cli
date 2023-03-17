using System.CommandLine;
using Unity.Services.Cli.Authoring.Input;
using Unity.Services.Cli.Common.Input;

namespace Unity.Services.Cli.Authoring.Export.Input;

/// <summary>
/// Class defining inputs specific to 'export' workflow
/// </summary>
public class ExportInput : DryRunInput
{
    /* Output directory for export command */
    public static readonly Argument<string> OutputDirectoryArgument = new("out-dir", "The output directory for export command");

    /* Output file name for export command */
    public static readonly Argument<string> FileNameArgument = new("file-name", "The output file name for export command")
    {
        Arity = ArgumentArity.ZeroOrOne
    };

    [InputBinding(nameof(OutputDirectoryArgument))]
    public string? OutputDirectory { get; set; }

    [InputBinding(nameof(FileNameArgument))]
    public string? FileName { get; set; }
}
