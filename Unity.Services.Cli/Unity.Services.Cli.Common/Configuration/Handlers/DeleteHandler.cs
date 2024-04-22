using Microsoft.Extensions.Logging;
using Unity.Services.Cli.Common.Exceptions;
using Unity.Services.Cli.Common.Input;
using Unity.Services.Cli.Common.Models;
using Unity.Services.Cli.Common.SystemEnvironment;

namespace Unity.Services.Cli.Common.Handlers;

static class DeleteHandler
{
    internal const string deletedAllKeysMsg = "All keys were deleted from local configuration.";
    internal const string deletedSpecifiedKeysMsg = "Specified keys were deleted from local configuration.";
    internal const string unsupportedOptionCombinationErrorMsg =
        $"Having both {ConfigurationInput.KeysLongAlias} and {ConfigurationInput.TargetAllKeysLongAlias} options " +
        "simultaneously is unsupported.";
    internal const string noOptionErrorMsg =
        $"Specify configuration keys to delete by using the {ConfigurationInput.KeysLongAlias} option. To delete " +
        $"all keys, use the {ConfigurationInput.TargetAllKeysLongAlias} option.";
    // Temporary, will be changed with EDX-1435
    internal const string forceRequiredErrorMsg =
        "This is a destructive operation, use the --force option to continue.";

    public static async Task DeleteAsync(
        ConfigurationInput input, IConfigurationService service, ILogger logger,
        ISystemEnvironmentProvider environmentProvider, CancellationToken cancellationToken)
    {
        if (input.TargetAllKeys is true && input.Keys is not null && input.Keys.Length is not 0)
        {
            throw new CliException(unsupportedOptionCombinationErrorMsg, ExitCode.HandledError);
        }

        if (input.TargetAllKeys is null or false && (input.Keys is null || input.Keys.Length is 0))
        {
            throw new CliException(noOptionErrorMsg, ExitCode.HandledError);

        }

        // Temporary, will be changed with EDX-1435
        if (!input.UseForce)
        {
            throw new CliException(forceRequiredErrorMsg, ExitCode.HandledError);
        }

        if (input.TargetAllKeys is true)
        {
            string[] keys = Keys.ConfigKeys.Keys.ToArray();
            await service.DeleteConfigArgumentsAsync(keys, cancellationToken);
            NotifyWhenKeysSetInEnvironmentVariables(environmentProvider, logger, keys);
            logger.LogInformation(deletedAllKeysMsg);
        }
        else
        {
            await service.DeleteConfigArgumentsAsync(input.Keys!, cancellationToken);
            NotifyWhenKeysSetInEnvironmentVariables(environmentProvider, logger, input.Keys!);
            logger.LogInformation(deletedSpecifiedKeysMsg);
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
