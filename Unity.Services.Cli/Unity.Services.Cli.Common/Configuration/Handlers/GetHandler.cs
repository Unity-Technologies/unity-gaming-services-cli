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
        string value;
        try
        {
            value = await service.GetConfigArgumentsAsync(input.Key ?? "", cancellationToken) ?? "";
        }
        catch (Exception)
        {
            Keys.ConfigEnvironmentPairs.TryGetValue(input.Key ?? "", out var environmentKey);

            if (!string.IsNullOrEmpty(environmentKey))
            {
                value = environmentProvider.GetSystemEnvironmentVariable(environmentKey, out _) ??
                    throw new MissingConfigurationException(input.Key ?? "", environmentKey);
            }
            else
            {
                throw;
            }
        }

        //Log operation result
        logger.LogResultValue(value);
    }
}
