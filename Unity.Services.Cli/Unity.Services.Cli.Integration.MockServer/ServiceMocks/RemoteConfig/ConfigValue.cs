namespace Unity.Services.Cli.MockServer.ServiceMocks.RemoteConfig;

[Serializable]
public struct ConfigValue
{
    public readonly string Key;
    public readonly ValueType Type;
    public readonly object Value;

    public ConfigValue(string key, ValueType type, object value)
    {
        Key = key;
        Type = type;
        Value = value;
    }
}
