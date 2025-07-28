using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Unity.Services.Cli.CloudSave.Input;
using Unity.Services.Cli.CloudSave.Models;
using Unity.Services.Cli.CloudSave.Service;
using Unity.Services.Cli.CloudSave.Utils;
using Unity.Services.Cli.Common.Console;
using Unity.Services.Cli.Common.Exceptions;
using Unity.Services.Cli.Common.Logging;
using Unity.Services.Cli.Common.Utils;

namespace Unity.Services.Cli.CloudSave.Handlers;

static class SetCustomDataItemHandler
{
    public static async Task SetCustomDataItemAsync(SetCustomItemInput input, IUnityEnvironment unityEnvironment, ICloudSaveDataService cloudSaveDataService, ILogger logger,
        ILoadingIndicator loadingIndicator, CancellationToken cancellationToken)
    {
        await loadingIndicator.StartLoadingAsync("Setting custom entity data...", _ =>
            SetCustomDataItemAsync(input, unityEnvironment, cloudSaveDataService, logger, cancellationToken));
    }

    internal static async Task SetCustomDataItemAsync(SetCustomItemInput input, IUnityEnvironment unityEnvironment, ICloudSaveDataService cloudSaveDataService,
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

        // If this is a valid json value, deserialize it into json. Otherwise, leave it as a string.
        object? jsonValue = input.Value != null && CanDeserialize(input.Value) ? JsonConvert.DeserializeObject(input.Value) : input.Value;

        var response = await cloudSaveDataService.SetCustomDataItemAsync(
            projectId: projectId,
            environmentId: environmentId,
            input.CustomId,
            input.Key,
            jsonValue,
            input.WriteLock,
            input.Visibility,
            cancellationToken: cancellationToken);

        logger.LogInformation(new SetPlayerDataItemOutput(response).ToString());
    }

    static bool CanDeserialize(string? value)
    {
        if (value == null)
        {
            return false;
        }
        try
        {
            JsonConvert.DeserializeObject(value);

        }
        catch (JsonException)
        {
            return false;
        }
        return true;
    }
}
