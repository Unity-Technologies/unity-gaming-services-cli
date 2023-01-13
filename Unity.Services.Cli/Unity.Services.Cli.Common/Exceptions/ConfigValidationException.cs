namespace Unity.Services.Cli.Common.Exceptions;

public class ConfigValidationException : CliException
{
    public string Key { get; }
    public string? Value { get; }

    public ConfigValidationException(string key, string? value, string message, int exitCode = Exceptions.ExitCode.HandledError)
        : base($"Your {key} is not valid. {message}", exitCode)
    {
        Key = key;
        Value = value;
    }
}
