using System.Net.Http.Headers;
using System.Reflection;

namespace Unity.Services.Cli.Common.Networking;

public static class RequestHeaderHelper
{
    public static string UserAgent => $"ugs_cli/{GetCliVersion()}";

    /// <summary>
    /// Key of the header, the header is used to identify where the service request come from.
    /// </summary>
#if ENABLE_UGS_CLI_TELEMETRY
    public const string XClientIdHeaderKey = "x-client-id";
#else
    public const string XClientIdHeaderKey = "x-client-id-staging";
#endif

    const string k_HeaderFeatureFlagKey = "X-Feature-Flag";
    const string k_HeaderFeatureFlagValue = "file-repo-v2";

    internal static string XClientIdHeaderValue { get; } = InitXClientIdHeaderValue();

    static string InitXClientIdHeaderValue()
    {
        var appName = GetCliName();
        var appVersion = GetCliVersion();
        return $"{appName}-cli@{appVersion}";
    }

    static string GetCliName()
    {
        var applicationAssemblyName = Assembly.GetEntryAssembly()!.GetName();
        var appName = applicationAssemblyName.Name!;
        return appName;
    }

    public static string GetCliVersion()
    {
        var versionAttribute = Attribute
                .GetCustomAttribute(
                    Assembly.GetEntryAssembly()!,
                    typeof(AssemblyInformationalVersionAttribute))
            as AssemblyInformationalVersionAttribute;
        return versionAttribute?.InformationalVersion ?? "";
    }

    /// <summary>
    /// Set the `x-client-id` header in the header collection.
    /// </summary>
    /// <param name="self">
    /// The header collection to add the header to.
    /// </param>
    /// <returns>
    /// Returns the header collection for fluent interface.
    /// </returns>
    public static IDictionary<string, string> SetXClientIdHeader(
        this IDictionary<string, string> self)
    {
        self[XClientIdHeaderKey] = XClientIdHeaderValue;
        return self;
    }

    /// <summary>
    /// Set the `x-client-id` header in the header collection.
    /// </summary>
    /// <param name="headers">
    /// The header collection to add the header to.
    /// </param>
    /// <returns>
    /// Returns the header collection for fluent interface.
    /// </returns>
    public static HttpRequestHeaders SetXClientIdHeader(this HttpRequestHeaders headers)
    {
        headers.Add(XClientIdHeaderKey, XClientIdHeaderValue);
        return headers;
    }



    /// <summary>
    /// Set the `X-Feature-Flag` header in the header collection to enable LiveContent inline variant data structure
    /// </summary>
    /// <param name="self">
    /// The header collection to add the header to.
    /// </param>
    /// <returns>
    /// Returns the header collection for fluent interface.
    /// </returns>
    public static void SetInlineVariantFeatureFlag(this IDictionary<string, string> self)
    {
        self[k_HeaderFeatureFlagKey] = k_HeaderFeatureFlagValue;
    }
}
