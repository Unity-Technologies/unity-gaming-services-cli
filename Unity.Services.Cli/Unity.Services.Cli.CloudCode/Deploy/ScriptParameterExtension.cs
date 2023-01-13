using Unity.Services.CloudCode.Authoring.Editor.Core.Model;
using Unity.Services.Gateway.CloudCodeApiV1.Generated.Model;

namespace Unity.Services.Cli.CloudCode.Deploy;

internal static class ScriptParameterExtension
{
    public static CloudCodeParameter ToCloudCodeParameter(this ScriptParameter scriptParameter)
    {
        return new CloudCodeParameter
        {
            Name = scriptParameter.Name,
            ParameterType = ConvertTypeOptionsToParamType(scriptParameter.Type),
            Required = scriptParameter.Required
        };
    }

    internal static ParameterType ConvertTypeOptionsToParamType(ScriptParameter.TypeEnum? parameterType)
    {
        switch (parameterType)
        {
            case ScriptParameter.TypeEnum.STRING:
                return ParameterType.String;
            case ScriptParameter.TypeEnum.NUMERIC:
                return ParameterType.Numeric;
            case ScriptParameter.TypeEnum.BOOLEAN:
                return ParameterType.Boolean;
            case ScriptParameter.TypeEnum.JSON:
                return ParameterType.JSON;
            case ScriptParameter.TypeEnum.ANY:
                return ParameterType.Any;
        }

        return default;
    }
}
