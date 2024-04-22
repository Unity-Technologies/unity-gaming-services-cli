using System.Runtime.Serialization;

namespace Unity.Services.Cli.Authoring.Exceptions;

/// <summary>
/// Exception when a user specified deploy path does not have expected extension
/// </summary>
public class InvalidExtensionException: DeployException
{
    public InvalidExtensionException(string path, string extension)
        : base($"File path must end in '{extension}': {path}") { }
}
