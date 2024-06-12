using System.CommandLine;

namespace Unity.Services.Cli.Common.Input;

public class ConfigurationInput : CommonInput
{
    [InputBinding(nameof(KeyArgument))]
    public string? Key { get; set; }

    public static readonly Argument<string> KeyArgument = new("key", "The key to get or set a configuration entry");

    [InputBinding(nameof(ValueArgument))]
    public string? Value { get; set; }

    public static readonly Argument<string> ValueArgument = new("value",
        "The value to set for the configuration entry identified by the <key> argument");

    [InputBinding(nameof(TargetAllKeysOption))]
    public bool? TargetAllKeys { get; set; }
    public const string TargetAllKeysLongAlias = "--all";

    public static readonly Option<bool> TargetAllKeysOption = new(
        new[]
        {
            "-a", TargetAllKeysLongAlias
        },
        "Target all keys in configuration"
    )
    {
        Arity = ArgumentArity.Zero,
    };

    [InputBinding(nameof(KeysOption))]
    public string[]? Keys { get; set; }
    public const string KeysLongAlias = "--key";

    public static readonly Option<string[]> KeysOption = new(
        new[]
        {
            "-k", KeysLongAlias
        },
        "A key in configuration"
    )
    {
        Arity = ArgumentArity.OneOrMore,
        AllowMultipleArgumentsPerToken = true
    };

    static ConfigurationInput()
    {
        KeyArgument.AddValidator(result =>
        {
            var key = result.GetValueOrDefault<string>();
            var allowedKeys = Models.Configuration.GetKeys();
            if (!allowedKeys.Contains(key))
            {
                result.ErrorMessage = $"key '{key}' not allowed. Allowed values: {string.Join(",", allowedKeys)}";
            }
        });
        KeyArgument.AddCompletions(_ => Models.Configuration.GetKeys().Where(key => key is not null)!);
    }
}
