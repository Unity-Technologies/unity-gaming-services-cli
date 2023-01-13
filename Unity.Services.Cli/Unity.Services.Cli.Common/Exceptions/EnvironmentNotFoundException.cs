namespace Unity.Services.Cli.Common.Exceptions;

public class EnvironmentNotFoundException : CliException
{
    public EnvironmentNotFoundException(string environmentName, int exitCode)
        : base($"The environment '{environmentName}' could not be found." +
            " Please re-enter a valid environment name.", exitCode)
    { }
}
