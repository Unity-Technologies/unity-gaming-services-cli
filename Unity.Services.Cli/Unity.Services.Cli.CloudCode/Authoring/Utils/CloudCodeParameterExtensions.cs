using Newtonsoft.Json;
using Unity.Services.CloudCode.Authoring.Editor.Core.Model;

namespace Unity.Services.Cli.CloudCode;

static class CloudCodeParameterExtensions
{
    public static string ToJavaScript(this IEnumerable<CloudCodeParameter> parameters)
    {
        var formattedParameters = parameters.ToDictionary(x => x.Name, CreateDictionaryValueFrom);
        var json = JsonConvert.SerializeObject(formattedParameters, Formatting.Indented);
        return json;

        object CreateDictionaryValueFrom(CloudCodeParameter parameter)
        {
            var parameterType = parameter.ParameterType.ToString().ToUpper();
            return parameter.Required
                ? new
                {
                    type = parameterType,
                    required = parameter.Required,
                }
                : parameterType;
        }
    }
}
