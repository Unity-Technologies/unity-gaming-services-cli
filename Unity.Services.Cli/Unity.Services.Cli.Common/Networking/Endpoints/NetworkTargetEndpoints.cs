namespace Unity.Services.Cli.Common.Networking;

/// <summary>
/// Base class to define endpoints for a network target.
/// </summary>
public abstract class NetworkTargetEndpoints
{
    /// <summary>
    /// URL to use when targeting production.
    /// </summary>
    protected abstract string Prod { get; }

    /// <summary>
    /// URL to use when targeting staging.
    /// </summary>
    protected abstract string Staging { get; }


    /// <summary>
    /// URL to use when targeting mock server.
    /// </summary>
    public static readonly string MockServer = "http://localhost:8080";

    public string Current
    {
        get
        {
#if USE_STAGING_ENDPOINTS
            return Staging;
#elif USE_MOCKSERVER_ENDPOINTS
            return MockServer;
#else
            return Prod;
#endif
        }
    }
}
