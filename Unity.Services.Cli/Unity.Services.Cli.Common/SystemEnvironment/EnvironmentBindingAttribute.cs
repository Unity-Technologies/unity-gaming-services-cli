namespace Unity.Services.Cli.Common.SystemEnvironment;

/// <summary>
/// An attribute to automatically assign a value from system environment variables to a property or field.
/// </summary>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public sealed class EnvironmentBindingAttribute : Attribute
{
    /// <summary>
    /// Name of the system environment variable key to assign to the decorated member.
    /// </summary>
    public string EnvironmentKey { get; }

    /// <summary>
    /// Create an attribute to assign a value from system environment variables to a field or a property.
    /// </summary>
    /// <param name="name">
    /// Name of the system environment variable key to assign to the decorated member.
    /// </param>
    public EnvironmentBindingAttribute(string environmentKey)
    {
        EnvironmentKey = environmentKey;
    }
}
