namespace Unity.Services.Cli.Common.Networking;

/// <summary>
/// Helper class to simplify network endpoint handling in the CLI.
/// </summary>
public static class EndpointHelper
{

    static readonly Dictionary<Type, NetworkTargetEndpoints> k_NetworkTargetEndpoints = new();

    internal static IReadOnlyDictionary<Type, NetworkTargetEndpoints> NetworkTargetEndpoints
        => k_NetworkTargetEndpoints;

    public static void InitializeNetworkTargetEndpoints(IEnumerable<NetworkTargetEndpoints> endpoints)
    {
        k_NetworkTargetEndpoints.Clear();
        foreach (var endpoint in endpoints)
        {
            k_NetworkTargetEndpoints[endpoint.GetType()] = endpoint;
        }
    }

    /// <summary>
    /// Get the endpoint for the current network environment for the network target of the given type.
    /// </summary>
    /// <typeparam name="TNetworkTarget">
    /// The type of network target to get endpoints for.
    /// </typeparam>
    /// <returns>
    /// Return the endpoint for the current network environment for the network target of the given type;
    /// throws otherwise.
    /// </returns>
    public static string GetCurrentEndpointFor<TNetworkTarget>()
        where TNetworkTarget : NetworkTargetEndpoints, new()
        => k_NetworkTargetEndpoints[typeof(TNetworkTarget)].Current;
}
