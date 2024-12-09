using System.Runtime.Serialization;
using Unity.Services.Cli.Common.Exceptions;

namespace Unity.Services.Cli.GameServerHosting.Exceptions;

public class InvalidConfigException : CliException
{
    public InvalidConfigException(string path)
        : base($"Multiplay Hosting Config file is invalid. See output for details: {path}", Common.Exceptions.ExitCode.HandledError) { }
}
