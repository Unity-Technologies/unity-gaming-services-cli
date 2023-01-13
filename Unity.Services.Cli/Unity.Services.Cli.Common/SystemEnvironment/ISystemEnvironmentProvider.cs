namespace Unity.Services.Cli.Common.SystemEnvironment;

public interface ISystemEnvironmentProvider
{
    /// <summary>
    /// Based on a given key, returns a value from system environment variable
    /// </summary>
    /// <param name="key">The key of the system environment variable to fetch</param>
    /// <param name="errorMsg">Error message generated while fetching from system environment variables</param>
    /// <returns>The value of the system environment variable that was fetched, null if the key is not set</returns>
    public string? GetSystemEnvironmentVariable(string key, out string errorMsg);
}
