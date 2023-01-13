using Microsoft.Extensions.Logging;
using Unity.Services.Cli.Common;
using Unity.Services.Cli.Common.Models;
using Unity.Services.Cli.Environment.Input;

namespace Unity.Services.Cli.Environment.Handlers;

static class UseHandler
{
    /// <summary>
    /// Sets the specified environment in the configuration service
    /// </summary>
    public static async Task UseAsync(
        EnvironmentInput input, IConfigurationService configService, ILogger logger,
        CancellationToken cancellationToken)
    {
        await configService.SetConfigArgumentsAsync(
            Keys.ConfigKeys.EnvironmentName, input.EnvironmentName!, cancellationToken);
        logger.LogInformation("Environment has been changed to '{EnvironmentName}'", input.EnvironmentName);
    }
}
