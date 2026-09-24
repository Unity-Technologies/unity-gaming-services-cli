namespace Unity.Services.Cli.Authoring.Templates;

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
    /// File body content written to disk by new-file (may use dynamic values).
    /// </summary>
    string FileBodyText { get; }

    /// <summary>
    /// File body content shown in --help-all output (must be deterministic).
    /// Defaults to <see cref="FileBodyText"/>; override when the two differ.
    /// </summary>
    string HelpBodyText => FileBodyText;
}
