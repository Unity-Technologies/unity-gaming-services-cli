namespace Unity.Services.Cli.Common.Networking;

/// <summary>
/// The different network environment supported by the CLI.
/// </summary>
public enum CliNetworkEnvironment
{
    /// <summary>
    /// Default environment.
    /// </summary>
    Production,
    /// <summary>
    /// Environment enabled when the USE_STAGING_ENDPOINTS constant is defined.
    /// </summary>
    Staging,
    /// <summary>
    /// Environment enabled when the USE_MOCKSERVER_ENDPOINTS constant is defined.
    /// </summary>
    MockServer,
}
