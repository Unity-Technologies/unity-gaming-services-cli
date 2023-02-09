namespace Unity.Services.Cli.Deploy.Templates;

/// <summary>
/// Interface to provide template for new file command
/// </summary>
public interface IFileTemplate
{
    /// <summary>
    /// File extension
    /// </summary>
    string Extension { get; }

    /// <summary>
    /// File body content
    /// </summary>
    string FileBodyText { get; }
}
