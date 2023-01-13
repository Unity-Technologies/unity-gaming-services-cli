using Microsoft.Extensions.Logging;
using Unity.Services.Cli.Common.Input;

namespace Unity.Services.Cli.Common.Handlers;

static class SetHandler
{
    public static async Task SetAsync(
        ConfigurationInput input, IConfigurationService service, ILogger logger, CancellationToken cancellationToken)
    {
        await service.SetConfigArgumentsAsync(input.Key ?? "", input.Value!, cancellationToken);

        logger.LogInformation("The config '{key}' has been set to '{value}'.", input.Key, input.Value);
    }
}
