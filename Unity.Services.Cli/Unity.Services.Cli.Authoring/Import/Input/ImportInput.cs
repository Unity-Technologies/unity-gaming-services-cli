using System.CommandLine;
using Unity.Services.Cli.Authoring.Input;
using Unity.Services.Cli.Common.Input;

namespace Unity.Services.Cli.Authoring.Import.Input;

/// <summary>
/// Class defining inputs specific to 'import' workflow
/// </summary>
public class ImportInput : DryRunInput
{
    static ImportInput()
    {
        ReconcileOption.SetDefaultValue(false);
    }

    /* Input directory for the import command */
    public static readonly Argument<string> InputDirectoryArgument = new("in-dir", "The input directory for import command");

    /* Input file name for import command */
    public static readonly Argument<string> FileNameArgument =
        new("file-name", "The input file name for import command")
        {
            Arity = ArgumentArity.ZeroOrOne
        };

    [InputBinding(nameof(InputDirectoryArgument))]
    public string? InputDirectory { get; set; }

    /* Option for reconcile */
    public static readonly Option<bool> ReconcileOption = new("--reconcile", "The command will delete existing configs before importing")
    {
        Arity = ArgumentArity.Zero
    };

    [InputBinding(nameof(ReconcileOption))]
    public bool Reconcile { get; set; }

    [InputBinding(nameof(FileNameArgument))]
    public string? FileName { get; set; }
}
