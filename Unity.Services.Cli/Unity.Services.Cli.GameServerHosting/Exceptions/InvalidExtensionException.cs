using System.Runtime.Serialization;
using Unity.Services.Cli.Common.Exceptions;

namespace Unity.Services.Cli.GameServerHosting.Exceptions;

[Serializable]
public class InvalidExtensionException : CliException
{
    public InvalidExtensionException(string path, string extension)
        : base($"File path must end in '{extension}': {path}", Common.Exceptions.ExitCode.HandledError) { }

    protected InvalidExtensionException(SerializationInfo info, StreamingContext context) : base(info, context) { }
}
