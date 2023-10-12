using Newtonsoft.Json.Linq;
using Unity.Services.Cli.Common.Exceptions;
using Unity.Services.Cli.RemoteConfig.Exceptions;
using Unity.Services.RemoteConfig.Editor.Authoring.Core.Formatting;

namespace Unity.Services.Cli.RemoteConfig.Deploy;

class ConfigTypeDeriver : IConfigTypeDeriver
{
    static readonly Dictionary<Type, ConfigType> k_TypeToConfigType = new()
    {
        [typeof(string)] = ConfigType.STRING,
        [typeof(int)] = ConfigType.INT,
        [typeof(bool)] = ConfigType.BOOL,
        [typeof(float)] = ConfigType.FLOAT,
        [typeof(double)] = ConfigType.FLOAT,
        [typeof(long)] = ConfigType.LONG,
        [typeof(JArray)] = ConfigType.JSON,
        [typeof(JObject)] = ConfigType.JSON
    };

    public ConfigType DeriveType(object obj)
    {
        var objectType = obj.GetType();

        if (!IsValidType(objectType))
        {
            var validConfigTypes = string.Join(", ", Enum.GetNames(typeof(ConfigType)));
            throw new ConfigTypeException($"The JSON key value type '{objectType}' is not supported for this " +
                                          $"service. Supported types are: {validConfigTypes}.", ExitCode.HandledError);
        }

        return k_TypeToConfigType[objectType];
    }

    static bool IsValidType(Type type)
        => k_TypeToConfigType.ContainsKey(type);
}
