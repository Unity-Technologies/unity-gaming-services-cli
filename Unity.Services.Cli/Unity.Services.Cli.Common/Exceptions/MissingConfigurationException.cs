namespace Unity.Services.Cli.Common.Exceptions;

public class MissingConfigurationException : CliException
{
    public string Key { get; }

    public MissingConfigurationException(
        string key, int exitCode = Common.Exceptions.ExitCode.HandledError)
        : base($"'{key}' is not set in project configuration.", exitCode)
    {
        Key = key;
    }

    public MissingConfigurationException(
        string configKey, string environmentKey, int exitCode = Common.Exceptions.ExitCode.HandledError)
        : base($"'{configKey}' is not set in project configuration." +
               $" '{environmentKey}' is not set in system environment variables.", exitCode)
    {
        Key = configKey;
    }
}
