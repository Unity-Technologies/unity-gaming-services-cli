using System.CommandLine;
using Unity.Services.Cli.Common.Input;
using Unity.Services.Cli.Common.SystemEnvironment;

namespace Unity.Services.Cli.Common.UnitTest;

class TestInput : CommonInput
{
    public const string EnvironmentBindingName = "UGS_CLI_TEST_KEY_0003";
    public const string ConfigBindingName = "test-key-0003";
    const string k_StringArgumentName = "string";

    [EnvironmentBinding(EnvironmentBindingName)]
    [ConfigBinding(ConfigBindingName)]
    [InputBinding(nameof(ValueStringArgument))]
    public string? StringArgValue { get; set; }

    public static readonly Argument<string> ValueStringArgument = new(k_StringArgumentName, "A string argument");

    const string k_IntArgumentName = "int";

    [InputBinding(nameof(ValueIntArgument))]
    public int IntArgValue { get; set; }

    public static readonly Argument<int> ValueIntArgument = new(k_IntArgumentName, "An int argument");

    [InputBinding(nameof(ValueIntOption))]
    public int IntOptionValue { get; set; }

    public static readonly Option<int> ValueIntOption = new(new[]
    {
        "-i",
        "--integer"
    }, "An integer option");

    [InputBinding(nameof(ValueStringOption))]
    public string? StringOptionValue = null;

    public static readonly Option<string> ValueStringOption = new(new[]
    {
        "-s",
        "--string"
    }, "A string option");
}
