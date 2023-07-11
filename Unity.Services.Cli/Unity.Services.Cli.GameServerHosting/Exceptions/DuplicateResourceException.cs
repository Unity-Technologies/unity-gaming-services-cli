using System.Runtime.Serialization;
using Unity.Services.Cli.Common.Exceptions;

namespace Unity.Services.Cli.GameServerHosting.Exceptions;

[Serializable]
public class DuplicateResourceException : CliException
{
    public DuplicateResourceException(string resource, string name)
        : base($"found duplicate {resource} of name {name}", Common.Exceptions.ExitCode.HandledError)
    {
    }

    protected DuplicateResourceException(SerializationInfo info, StreamingContext context) : base(info, context) { }
}
