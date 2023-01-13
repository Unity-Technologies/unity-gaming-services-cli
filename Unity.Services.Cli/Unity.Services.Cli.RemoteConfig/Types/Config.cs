namespace Unity.Services.Cli.RemoteConfig.Types;

public enum ValueType
{
    String,
    Int,
    Bool,
    Float,
    Long,
    Json,
}

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
