namespace Unity.Services.Cli.Common.Utils;

/// <summary>
/// Utility to facilitate mapping between unity environment id and name
/// </summary>
public interface IUnityEnvironment
{
    public void SetName(string? value);

    public void SetProjectId(string? value);

    /// <summary>
    /// Gets the Unity environment id from the current saved environment name
    /// </summary>
    /// <returns>environment id</returns>
    public Task<string> FetchIdentifierAsync();
    /// <summary>
    /// Gets the Unity environment id from that matches the environment name passed as parameter
    /// </summary>
    /// <param name="environmentName"></param>
    /// <returns></returns>
    public Task<string> FetchIdentifierFromSpecificEnvironmentNameAsync(string environmentName);
}
