using System.Reflection;

namespace Unity.Services.Cli.Common.Networking;

/// <summary>
/// Helper class to simplify network endpoint handling in the CLI.
/// </summary>
public static class EndpointHelper
{
    /// <summary>
    /// The network environment currently targeted by the CLI.
    /// </summary>
    public static CliNetworkEnvironment CurrentNetworkEnvironment
    {
        get
        {
#if USE_STAGING_ENDPOINTS
            return CliNetworkEnvironment.Staging;
#elif USE_MOCKSERVER_ENDPOINTS
            return CliNetworkEnvironment.MockServer;
#else
            return CliNetworkEnvironment.Production;
#endif
        }
    }

    static readonly Dictionary<Type, NetworkTargetEndpoints> k_NetworkTargetEndpoints = new();

    internal static IReadOnlyDictionary<Type, NetworkTargetEndpoints> NetworkTargetEndpoints
        => k_NetworkTargetEndpoints;

    public static void InitializeNetworkTargetEndpoints(IEnumerable<TypeInfo> definedTypes)
    {
        k_NetworkTargetEndpoints.Clear();
        foreach (var definedType in definedTypes)
        {
            if (definedType.IsAbstract
                || definedType.IsGenericType
                || !definedType.IsAssignableTo(typeof(NetworkTargetEndpoints)))
            {
                continue;
            }

            k_NetworkTargetEndpoints[definedType] = (NetworkTargetEndpoints)Activator.CreateInstance(definedType)!;
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
