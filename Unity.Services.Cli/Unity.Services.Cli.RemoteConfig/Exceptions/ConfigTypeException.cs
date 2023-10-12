using Unity.Services.Cli.Common.Exceptions;

namespace Unity.Services.Cli.RemoteConfig.Exceptions;

public class ConfigTypeException : CliException
{
    public ConfigTypeException(string message, int exitCode)
        : base(message, exitCode) { }
}
