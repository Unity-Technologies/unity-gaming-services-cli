using Unity.Services.Cli.CloudCode.Deploy;
using Unity.Services.Cli.CloudCode.Service;
using Unity.Services.CloudCode.Authoring.Editor.Core.Model;
using Unity.Services.Gateway.CloudCodeApiV1.Generated.Model;

namespace Unity.Services.Cli.CloudCode.Handlers.ImportExport.Scripts;

static class ImportExportUtils
{
    public static async Task<IEnumerable<CloudCodeScript>> GetScriptDetails(
        string projectId,
        string environmentId,
        IEnumerable<String> scriptNames,
        ICloudCodeService cloudCodeService,
        CancellationToken cancellationToken)
    {
        var scriptsWithData = new List<CloudCodeScript>();
        foreach (var scriptName in scriptNames)
        {
            var remoteScript = await cloudCodeService.GetAsync(
                projectId, environmentId, scriptName, cancellationToken);
            var newScriptFromRemote = new CloudCodeScript(remoteScript);
            scriptsWithData.Add(newScriptFromRemote);
        }

        return scriptsWithData;
    }

    public static IReadOnlyList<ScriptParameter> ConvertAuthoringParamsToParams(List<CloudCodeParameter> parameters)
    {
        // convert parameters to CLI parameters
        var cliParameters = new List<ScriptParameter>();
        foreach (var parameter in parameters)
        {
            cliParameters.Add(new ScriptParameter(
                parameter.Name,
                ConvertTypeOptionsToParamType(parameter.ParameterType),
                parameter.Required));
        }

        return cliParameters;
    }

    static ScriptParameter.TypeEnum ConvertTypeOptionsToParamType(ParameterType parameterType)
    {
        switch (parameterType)
        {
            case ParameterType.String:
                return ScriptParameter.TypeEnum.STRING;
            case ParameterType.Numeric:
                return ScriptParameter.TypeEnum.NUMERIC;
            case ParameterType.Boolean:
                return ScriptParameter.TypeEnum.BOOLEAN;
            case ParameterType.JSON:
                return ScriptParameter.TypeEnum.JSON;
            case ParameterType.Any:
                return ScriptParameter.TypeEnum.ANY;
        }

        return default;
    }
}
