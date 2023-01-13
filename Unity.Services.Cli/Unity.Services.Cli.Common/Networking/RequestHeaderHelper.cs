using System.Net.Http.Headers;
using System.Reflection;

namespace Unity.Services.Cli.Common.Networking;

public static class RequestHeaderHelper
{
    /// <summary>
    /// Key of the header, the header is used to identify where the service request come from.
    /// </summary>
    public const string XClientIdHeaderKey = "x-client-id";

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
}
