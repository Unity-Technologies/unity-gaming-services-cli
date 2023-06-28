using Unity.Services.Cli.CloudCode.Exceptions;
using Unity.Services.Cli.CloudCode.Parameters;
using Unity.Services.CloudCode.Authoring.Editor.Core.Model;

namespace Unity.Services.Cli.CloudCode;

static class CloudCodeScriptParserExtensions
{
    internal const string ErrorMessageFormat = "Script {0} threw:{1}\"{2}\".";

    public static async Task<(bool HasParameters, string? ErrorMessage)>
        TryParseScriptParametersAsync(this ICloudCodeScriptParser self, IScript script, CancellationToken token)
    {
        string? errorMessage = null;
        bool scriptContainsParametersJson = false;

        try
        {
            var paramsParsingResponse = await self.ParseScriptParametersAsync(script.Body, token);
            scriptContainsParametersJson = paramsParsingResponse.ScriptContainsParametersJson;
        }
        catch (ScriptEvaluationException e)
        {
            errorMessage = string.Format(ErrorMessageFormat, script.Name.ToString(), System.Environment.NewLine, e.Message);
        }

        return (scriptContainsParametersJson, errorMessage);
    }
}
