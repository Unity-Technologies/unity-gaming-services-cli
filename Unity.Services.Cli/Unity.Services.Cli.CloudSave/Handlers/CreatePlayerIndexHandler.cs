using Microsoft.Extensions.Logging;
using Unity.Services.Cli.CloudSave.Input;
using Unity.Services.Cli.CloudSave.Models;
using Unity.Services.Cli.CloudSave.Service;
using Unity.Services.Cli.CloudSave.Utils;
using Unity.Services.Cli.Common.Console;
using Unity.Services.Cli.Common.Exceptions;
using Unity.Services.Cli.Common.Logging;
using Unity.Services.Cli.Common.Utils;

namespace Unity.Services.Cli.CloudSave.Handlers;

static class CreatePlayerIndexHandler
{
    public static async Task CreatePlayerIndexAsync(CreateIndexInput input, IUnityEnvironment unityEnvironment, ICloudSaveDataService cloudSaveDataService, ILogger logger,
        ILoadingIndicator loadingIndicator, CancellationToken cancellationToken)
    {
        await loadingIndicator.StartLoadingAsync("Fetching resources...", _ =>
            CreatePlayerIndexAsync(input, unityEnvironment, cloudSaveDataService, logger, cancellationToken));
    }

    internal static async Task CreatePlayerIndexAsync(CreateIndexInput input, IUnityEnvironment unityEnvironment, ICloudSaveDataService cloudSaveDataService,
        ILogger logger, CancellationToken cancellationToken)
    {
        var projectId = input.CloudProjectId!;
        var environmentId = await unityEnvironment.FetchIdentifierAsync(cancellationToken);

        if (string.IsNullOrEmpty(input.Visibility))
        {
            input.Visibility = PlayerIndexVisibilityTypes.Default;
        }

        if (!PlayerIndexVisibilityTypes.IsValidType(input.Visibility))
        {
            throw new CliException($"Invalid visibility option: {input.Visibility}. Valid options are: {string.Join(", ", PlayerIndexVisibilityTypes.GetTypes())}", ExitCode.HandledError);
        }

        var response = await cloudSaveDataService.CreatePlayerIndexAsync(
            projectId: projectId,
            environmentId: environmentId,
            input.Fields,
            input.Visibility,
            RequestBodyHandler.GetRequestBodyFromFileOrInput(input.JsonFileOrBody, isRequired: input.Fields == null),
            cancellationToken: cancellationToken);

        logger.LogResultValue(new CreateIndexOutput(response));
    }
}
