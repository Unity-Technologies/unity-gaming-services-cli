using System.Runtime.Serialization;

namespace Unity.Services.Cli.Authoring.Exceptions;

/// <summary>
/// Exception when a user specified deploy path is not found
/// </summary>
public class PathNotFoundException: DeployException
{
    public PathNotFoundException(string path)
        : base($"Path {path} could not be found.") { }
}
