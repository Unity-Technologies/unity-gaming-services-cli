using System.Runtime.Serialization;

namespace Unity.Services.Cli.Authoring.Exceptions;

/// <summary>
/// Exception when a user specified deploy path is not found
/// </summary>
[Serializable]
public class PathNotFoundException: DeployException
{
    public PathNotFoundException(string path)
        : base($"Path {path} could not be found.") { }

    protected PathNotFoundException(SerializationInfo info, StreamingContext context) : base(info, context) { }
}
