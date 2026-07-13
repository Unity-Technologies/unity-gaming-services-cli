using Microsoft.Extensions.Logging;
using Unity.Services.Cli.Common.Exceptions;
using Unity.Services.Cli.Common.Input;
using Unity.Services.Cli.Common.Logging;
using Unity.Services.Cli.Common.Models;
using Unity.Services.Cli.Common.SystemEnvironment;

namespace Unity.Services.Cli.Common.Handlers;

static class GetHandler
{
    public static async Task GetAsync(
        ConfigurationInput input, IConfigurationService service, ISystemEnvironmentProvider environmentProvider,
        ILogger logger, CancellationToken cancellationToken)
    {
        string? value = null;

        if (Keys.ConfigEnvironmentPairs.TryGetValue(input.Key ?? "", out var environmentKey))
        {
            value = environmentProvider.GetSystemEnvironmentVariable(environmentKey, out _);
            if (!string.IsNullOrWhiteSpace(value))
            {
                logger.LogInformation($"Environment variable {environmentKey} has a value. It will be used instead of the saved configuration.");
            }
            else
            {
                value = null; // reset value in case it is string.Empty or whitespace
            }
        }

        if (value == null)
        {
            try
            {
                value = await service.GetConfigArgumentsAsync(input.Key ?? "", cancellationToken) ?? "";
            }
            catch (Exception)
            {
                if (!string.IsNullOrEmpty(environmentKey))
                {
                    throw new MissingConfigurationException(input.Key ?? "", environmentKey);
                }
                throw;
            }
        }

        //Log operation result
        logger.LogResultValue(value);
    }
}
