using Microsoft.Extensions.Logging;
using Unity.Services.Cli.CloudSave.Input;
using Unity.Services.Cli.CloudSave.Service;
using Unity.Services.Cli.CloudSave.Utils;
using Unity.Services.Cli.Common.Console;
using Unity.Services.Cli.Common.Exceptions;
using Unity.Services.Cli.Common.Logging;
using Unity.Services.Cli.Common.Utils;

namespace Unity.Services.Cli.CloudSave.Handlers;

static class QueryCustomDataHandler
{
    public static async Task QueryCustomDataAsync(QueryDataInput input, IUnityEnvironment unityEnvironment, ICloudSaveDataService cloudSaveDataService, ILogger logger,
        ILoadingIndicator loadingIndicator, CancellationToken cancellationToken)
    {
        await loadingIndicator.StartLoadingAsync("Fetching resources...", _ =>
            QueryCustomDataAsync(input, unityEnvironment, cloudSaveDataService, logger, cancellationToken));
    }

    internal static async Task QueryCustomDataAsync(QueryDataInput input, IUnityEnvironment unityEnvironment, ICloudSaveDataService cloudSaveDataService,
        ILogger logger, CancellationToken cancellationToken)
    {
        var projectId = input.CloudProjectId!;
        var environmentId = await unityEnvironment.FetchIdentifierAsync(cancellationToken);

        if (string.IsNullOrEmpty(input.Visibility))
        {
            input.Visibility = CustomIndexVisibilityTypes.Default;
        }

        if (!CustomIndexVisibilityTypes.IsValidType(input.Visibility))
        {
            throw new CliException($"Invalid visibility option: {input.Visibility}. Valid options are: {string.Join(", ", CustomIndexVisibilityTypes.GetTypes())}", ExitCode.HandledError);
        }

        var response = await cloudSaveDataService.QueryCustomDataAsync(
            projectId: projectId,
            environmentId: environmentId,
            input.Visibility,
            RequestBodyHandler.GetRequestBodyFromFileOrInput(input.JsonFileOrBody, isRequired: true),
            cancellationToken: cancellationToken);

        logger.LogResultValue(input.IsJson ? response : response.ToJson());
    }
}
