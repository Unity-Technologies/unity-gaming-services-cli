using System.Runtime.Serialization;
using Unity.Services.Cli.Common.Exceptions;

namespace Unity.Services.Cli.GameServerHosting.Exceptions;

[Serializable]
public class PathNotFoundException : CliException
{
    public PathNotFoundException(string path)
        : base($"File path {path} could not be found.", Common.Exceptions.ExitCode.HandledError) { }

    protected PathNotFoundException(SerializationInfo info, StreamingContext context) : base(info, context) { }
}
