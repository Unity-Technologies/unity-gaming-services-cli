using Unity.Services.Gateway.EconomyApiV2.Generated.Client;

namespace Unity.Services.Cli.Economy.Exceptions;

[Serializable]
public class InvalidResourceException : ApiException
{
    public InvalidResourceException(string message, Exception innerException)
        : base(Common.Exceptions.ExitCode.HandledError, $"Economy resource file is invalid: {message}", innerException) { }
}
