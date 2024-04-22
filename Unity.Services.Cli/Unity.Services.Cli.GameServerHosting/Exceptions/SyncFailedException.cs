using System.Runtime.Serialization;
using Unity.Services.Cli.Common.Exceptions;

namespace Unity.Services.Cli.GameServerHosting.Exceptions;

public class SyncFailedException : CliException
{
    public SyncFailedException()
        : base("failed to sync build", Common.Exceptions.ExitCode.HandledError)
    {
    }
}
