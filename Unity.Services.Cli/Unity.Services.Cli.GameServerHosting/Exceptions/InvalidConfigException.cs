using System.Runtime.Serialization;
using Unity.Services.Cli.Common.Exceptions;

namespace Unity.Services.Cli.GameServerHosting.Exceptions;

[Serializable]
public class InvalidConfigException : CliException
{
    public InvalidConfigException(string path)
        : base($"Game Server Hosting Config file is invalid. See output for details: {path}", Common.Exceptions.ExitCode.HandledError) { }

    protected InvalidConfigException(SerializationInfo info, StreamingContext context) : base(info, context) { }
}
