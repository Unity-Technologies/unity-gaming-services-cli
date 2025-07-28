using System.CommandLine;
using Unity.Services.Cli.Common.Input;

namespace Unity.Services.Cli.GameServerHosting.Input;

public class SimulatorInput : CommonInput
{
    public static readonly Option<string> ManageProcessOption = new(
        new[]
        {
            "-m",
            "--manage-process"
        },
        "Path to executable (and arguments) to run on allocation. Allows you to test the process management.")
    {
        Arity = ArgumentArity.ZeroOrOne
    };

    public static readonly Option<bool> UseA2SOption = new(
        new[]
        {
            "--use-a2s"
        },
        "Use the A2S protocol instead of SQP when querying server info.")
    {
        Arity = ArgumentArity.Zero
    };

    [InputBinding(nameof(ManageProcessOption))]
    public string? ManageProcess { get; set; }

    [InputBinding(nameof(UseA2SOption))]
    public bool UseA2S { get; set; }
}
