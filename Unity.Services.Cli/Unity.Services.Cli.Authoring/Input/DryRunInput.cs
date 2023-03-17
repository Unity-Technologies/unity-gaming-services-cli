using System.CommandLine;
using Unity.Services.Cli.Common.Input;

namespace Unity.Services.Cli.Authoring.Input;

/// <summary>
/// Class defining inputs for dry run.
/// </summary>
public class DryRunInput : CommonInput
{
    static DryRunInput()
    {
        DryRunOption.SetDefaultValue(false);
    }

    /* Option for dry run */
    public static readonly Option<bool> DryRunOption = new("--dry-run", "The command will do a dry run")
    {
        Arity = ArgumentArity.Zero
    };

    [InputBinding(nameof(DryRunOption))]
    public bool DryRun { get; set; }
}
