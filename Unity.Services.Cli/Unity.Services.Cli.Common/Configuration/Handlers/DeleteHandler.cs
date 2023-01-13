using Microsoft.Extensions.Logging;
using Unity.Services.Cli.Common.Exceptions;
using Unity.Services.Cli.Common.Input;
using Unity.Services.Cli.Common.Models;
using Unity.Services.Cli.Common.SystemEnvironment;

namespace Unity.Services.Cli.Common.Handlers;

static class DeleteHandler
{
    internal const string k_DeletedAllKeysMsg = "All keys were deleted from local configuration.";
    internal const string k_DeletedSpecifiedKeysMsg = "Specified keys were deleted from local configuration.";
    internal const string k_UnsupportedOptionCombinationErrorMsg =
        $"Having both {ConfigurationInput.KeysLongAlias} and {ConfigurationInput.TargetAllKeysLongAlias} options " +
        "simultaneously is unsupported.";
    internal const string k_NoOptionErrorMsg =
        $"Specify configuration keys to delete by using the {ConfigurationInput.KeysLongAlias} option. To delete " +
        $"all keys, use the {ConfigurationInput.TargetAllKeysLongAlias} option.";
    // Temporary, will be changed with EDX-1435
    internal const string k_ForceRequiredErrorMsg =
        "This is a destructive operation, use the --force option to continue.";

    public static async Task DeleteAsync(
        ConfigurationInput input, IConfigurationService service, ILogger logger,
        ISystemEnvironmentProvider environmentProvider, CancellationToken cancellationToken)
    {
        if (input.TargetAllKeys is true && input.Keys is not null && input.Keys.Length is not 0)
        {
            throw new CliException(k_UnsupportedOptionCombinationErrorMsg, ExitCode.HandledError);
        }

        if (input.TargetAllKeys is null or false && (input.Keys is null || input.Keys.Length is 0))
        {
            throw new CliException(k_NoOptionErrorMsg, ExitCode.HandledError);

        }

        // Temporary, will be changed with EDX-1435
        if (!input.UseForce)
        {
            throw new CliException(k_ForceRequiredErrorMsg, ExitCode.HandledError);
        }

        if (input.TargetAllKeys is true)
        {
            string[] keys = Keys.ConfigKeys.Keys.ToArray();
            await service.DeleteConfigArgumentsAsync(keys, cancellationToken);
            NotifyWhenKeysSetInEnvironmentVariables(environmentProvider, logger, keys);
            logger.LogInformation(k_DeletedAllKeysMsg);
        }
        else
        {
            await service.DeleteConfigArgumentsAsync(input.Keys!, cancellationToken);
            NotifyWhenKeysSetInEnvironmentVariables(environmentProvider, logger, input.Keys!);
            logger.LogInformation(k_DeletedSpecifiedKeysMsg);
        }
    }

    static void NotifyWhenKeysSetInEnvironmentVariables(ISystemEnvironmentProvider environmentProvider, ILogger logger,
        string[] keys)
    {
        foreach (string key in keys)
        {
            if (Keys.ConfigEnvironmentPairs.TryGetValue(key, out string? value))
            {
                string? envValue = environmentProvider.GetSystemEnvironmentVariable(value, out _);

                if (!string.IsNullOrEmpty(envValue))
                {
                    logger.LogWarning("Key '{key}' was deleted from local configuration, " +
                                      "but is still set in environment variable '{value}'.", key, value);
                }
            }
        }
    }
}
