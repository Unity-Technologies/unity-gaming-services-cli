using Microsoft.Extensions.Logging;
using Spectre.Console;
using Unity.Services.Cli.CloudCode.Input;
using Unity.Services.Cli.CloudCode.Parameters;
using Unity.Services.Cli.CloudCode.Service;
using Unity.Services.Cli.Common.Console;
using Unity.Services.Cli.Common.Utils;

namespace Unity.Services.Cli.CloudCode.Handlers;

static class UpdateHandler
{
    public static async Task UpdateAsync(
        CloudCodeInput input,
        IUnityEnvironment unityEnvironment,
        ICloudCodeService cloudCodeService,
        ICloudCodeInputParser cloudCodeInputParser,
        ILogger logger,
        ILoadingIndicator loadingIndicator,
        CancellationToken cancellationToken)
    {
        await loadingIndicator.StartLoadingAsync(
            "Updating script...",
            context => UpdateAsync(
                input, unityEnvironment, cloudCodeService, cloudCodeInputParser, logger, context, cancellationToken));
    }

    internal static async Task UpdateAsync(
        CloudCodeInput input,
        IUnityEnvironment unityEnvironment,
        ICloudCodeService cloudCodeService,
        ICloudCodeInputParser cloudCodeInputParser,
        ILogger logger,
        StatusContext? loadingContext,
        CancellationToken cancellationToken)
    {
        var environmentId = await unityEnvironment.FetchIdentifierAsync(cancellationToken);
        var projectId = input.CloudProjectId!;

        loadingContext?.Status("Loading script...");
        var code = await cloudCodeInputParser.LoadScriptCodeAsync(input, cancellationToken);
        var parametersParsingResult = await cloudCodeInputParser.CloudCodeScriptParser
            .ParseScriptParametersAsync(code, cancellationToken);
        loadingContext?.Status("Uploading script...");
        await cloudCodeService.UpdateAsync(
            projectId,
            environmentId,
            input.ScriptName,
            code,
            parametersParsingResult.Parameters,
            CancellationToken.None);

        logger.LogInformation("Script '{scriptName}' updated.", input.ScriptName);
    }
}
