using System.Security;

namespace Unity.Services.Cli.Common.SystemEnvironment;

public class SystemEnvironmentProvider : ISystemEnvironmentProvider
{
    internal const string SecurityExceptionMessage = "Could not fetch from system environment variables because CLI" +
                                                     " does not have the required permissions.";

    /// <inheritdoc cref="ISystemEnvironmentProvider.GetSystemEnvironmentVariable"/>
    public string? GetSystemEnvironmentVariable(string key, out string errorMsg)
    {
        errorMsg = "";

        try
        {
            string? value = Environment.GetEnvironmentVariable(key);

            if (String.IsNullOrEmpty(value))
            {
                errorMsg = $"{key} is not set in system environment variables.";
            }

            return value;
        }
        catch (SecurityException e)
        {
            throw new SecurityException(SecurityExceptionMessage, e);
        }
    }
}
