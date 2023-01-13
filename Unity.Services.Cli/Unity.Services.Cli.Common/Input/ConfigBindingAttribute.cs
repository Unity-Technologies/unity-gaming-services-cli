namespace Unity.Services.Cli.Common.Input;

/// <summary>
/// An attribute to automatically bind a value from configuration to a property or field.
/// </summary>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public sealed class ConfigBindingAttribute : Attribute
{
    /// <summary>
    /// Name of the configuration key to assign to the decorated member.
    /// </summary>
    public string ConfigName { get; }

    /// <summary>
    /// Create an attribute to assign a value from configuration to a field or a property.
    /// </summary>
    /// <param name="name">
    /// Name of the configuration key to assign to the decorated member.
    /// </param>
    public ConfigBindingAttribute(string name)
    {
        ConfigName = name;
    }
}
