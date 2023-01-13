namespace Unity.Services.Cli.Common.Input;

/// <summary>
/// An attribute to automatically bind a command input to a property or field.
/// </summary>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public class InputBindingAttribute : Attribute
{
    /// <summary>
    /// Name of the input to bind to the decorated member.
    /// </summary>
    public string InputName { get; }

    /// <summary>
    /// Create an attribute to bind an input to a field or a property.
    /// </summary>
    /// <param name="name">
    /// Name of the input to bind to the decorated member.
    /// </param>
    public InputBindingAttribute(string name)
    {
        InputName = name;
    }
}
