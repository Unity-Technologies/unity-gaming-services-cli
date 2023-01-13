using Newtonsoft.Json.Linq;
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
        [typeof(JObject)] = ConfigType.JSON
    };

    public ConfigType DeriveType(object obj)
    {
        return k_TypeToConfigType[obj.GetType()];
    }
}
