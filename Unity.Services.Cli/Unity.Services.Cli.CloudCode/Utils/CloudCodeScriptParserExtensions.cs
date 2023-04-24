using Unity.Services.Cli.CloudCode.Exceptions;
using Unity.Services.Cli.CloudCode.Parameters;
using Unity.Services.CloudCode.Authoring.Editor.Core.Model;
using Unity.Services.Gateway.CloudCodeApiV1.Generated.Model;

namespace Unity.Services.Cli.CloudCode;

static class CloudCodeScriptParserExtensions
{
    internal const string ErrorMessageFormat = "Script {0} threw:{1}\"{2}\".";

    public static async Task<(bool HasParameters, string? ErrorMessage)> TryParseScriptParametersAsync(
        this ICloudCodeScriptParser self, IScript script, CancellationToken token)
    {
        IReadOnlyList<ScriptParameter>? scriptParams;
        string? errorMessage = null;
        try
        {
            scriptParams = await self.ParseScriptParametersAsync(script.Body, token);
        }
        catch (ScriptEvaluationException e)
        {
            errorMessage = string.Format(ErrorMessageFormat, script.Name.ToString(), System.Environment.NewLine, e.Message);
            scriptParams = null;
        }

        return ((scriptParams is not null) && (scriptParams.Count > 0), errorMessage);
    }
}
