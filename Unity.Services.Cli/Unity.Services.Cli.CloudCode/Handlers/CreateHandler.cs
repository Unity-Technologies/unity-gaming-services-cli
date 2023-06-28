using Microsoft.Extensions.Logging;
using Spectre.Console;
using Unity.Services.Cli.CloudCode.Input;
using Unity.Services.Cli.CloudCode.Parameters;
using Unity.Services.Cli.CloudCode.Service;
using Unity.Services.Cli.Common.Console;
using Unity.Services.Cli.Common.Utils;

namespace Unity.Services.Cli.CloudCode.Handlers;

static class CreateHandler
{
    public static async Task CreateAsync(
        CloudCodeInput input,
        IUnityEnvironment unityEnvironment,
        ICloudCodeService cloudCodeService,
        ICloudCodeInputParser cloudCodeInputParser,
        ILogger logger,
        ILoadingIndicator loadingIndicator,
        CancellationToken cancellationToken)
    {
        await loadingIndicator.StartLoadingAsync(
            "Creating script...",
            context => CreateAsync(
                input, unityEnvironment, cloudCodeService, cloudCodeInputParser, logger, context, cancellationToken));
    }

    internal static async Task CreateAsync(
        CloudCodeInput input,
        IUnityEnvironment unityEnvironment,
        ICloudCodeService cloudCodeService,
        ICloudCodeInputParser cloudCodeInputParser,
        ILogger logger,
        StatusContext? loadingContext,
        CancellationToken cancellationToken)
    {
        var environmentId = await unityEnvironment.FetchIdentifierAsync();
        var projectId = input.CloudProjectId!;

        loadingContext?.Status("Loading script...");

        var scriptType = cloudCodeInputParser.ParseScriptType(input);
        var scriptLanguage = cloudCodeInputParser.ParseLanguage(input);
        var code = await cloudCodeInputParser.LoadScriptCodeAsync(input, cancellationToken);
        var parametersParsingResult = await cloudCodeInputParser.CloudCodeScriptParser
            .ParseScriptParametersAsync(code, cancellationToken);
        loadingContext?.Status("Uploading script...");

        await cloudCodeService.CreateAsync(
            projectId, environmentId,
            input.ScriptName, scriptType, scriptLanguage,
            code, parametersParsingResult.Parameters, cancellationToken);

        logger.LogInformation("Script '{scriptName}' created.", input.ScriptName);
    }
}
