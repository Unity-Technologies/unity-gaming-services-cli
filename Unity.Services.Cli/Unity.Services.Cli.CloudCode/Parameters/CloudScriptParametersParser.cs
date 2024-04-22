using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Unity.Services.Cli.CloudCode.Exceptions;
using Unity.Services.Gateway.CloudCodeApiV1.Generated.Model;

namespace Unity.Services.Cli.CloudCode.Parameters;

class CloudScriptParametersParser : ICloudScriptParametersParser
{
    static string ParamTypePossibleValues => string.Join(", ", Enum.GetNames(typeof(ScriptParameter.TypeEnum)));

    struct EvaluatedParam
    {
        public ScriptParameter.TypeEnum Type = ScriptParameter.TypeEnum.ANY;
        public bool Required = false;

        public EvaluatedParam() { }
    }

    public List<ScriptParameter> ParseToScriptParameters(string parameterJsonString)
    {
        try
        {
            var parameters = JObject.Parse(parameterJsonString!);

            var parsedParams = new List<ScriptParameter>();
            foreach (var symbol in parameters)
            {
                var paramName = symbol.Key;
                var cloudCodeParam = new ScriptParameter(paramName);
                ParseParameter(symbol.Value!, cloudCodeParam);
                parsedParams.Add(cloudCodeParam);
            }
            return parsedParams;
        }
        catch (JsonReaderException ex)
        {
            throw new ScriptEvaluationException(ex.Message);
        }
    }

    static void ParseParameter(JToken param, ScriptParameter result)
    {
        switch (param)
        {
            case JValue:
                ParseValue(param, result);
                break;
            case JObject jParamData:
                ParseObject(jParamData, result);
                break;
            default:
                throw new ScriptEvaluationException($"Type of {param} is not supported: {param.GetType()}");
        }
    }

    static void ParseValue(JToken param, ScriptParameter result)
    {
        try
        {
            ScriptParameter.TypeEnum type = param.ToObject<ScriptParameter.TypeEnum>();
            result.Type = type;
        }
        catch (JsonSerializationException ex)
        {
            throw new ScriptEvaluationException(ex.Message, ex);
        }
        catch (ArgumentException ex)
        {
            throw new ScriptEvaluationException($"Impossible to convert '{param}' to enum. Possible values are: {ParamTypePossibleValues}.", ex);
        }
    }

    static void ParseObject(JObject jParamData, ScriptParameter result)
    {
        try
        {
            var paramData = jParamData.ToObject<EvaluatedParam>();
            result.Type = paramData.Type;
            result.Required = paramData.Required;
        }
        catch (JsonSerializationException ex)
        {
            if (ex.Path?.EndsWith(".type") ?? false)
            {
                throw new ScriptEvaluationException($"Impossible to convert '{jParamData.GetValue("type")}' to enum. Possible values are: {ParamTypePossibleValues}.", ex);
            }
            throw new ScriptEvaluationException(ex.Message);
        }
    }
}
