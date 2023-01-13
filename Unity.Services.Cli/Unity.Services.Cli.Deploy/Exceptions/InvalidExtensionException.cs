using System.Runtime.Serialization;

namespace Unity.Services.Cli.Deploy.Exceptions;

/// <summary>
/// Exception when a user specified deploy path does not have expected extension
/// </summary>
[Serializable]
public class InvalidExtensionException: DeployException
{
    public InvalidExtensionException(string path, string extension)
        : base($"File path must end in '{extension}': {path}") { }

    protected InvalidExtensionException(SerializationInfo info, StreamingContext context) : base(info, context) { }
}
