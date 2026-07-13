namespace Unity.Services.Cli.ServiceAccountAuthentication.Token;

public static class AccessTokenHelper
{
    /// <summary>
    /// The key of the access token used in a request header.
    /// </summary>
    public const string HeaderKey = "Authorization";

    /// <summary>
    /// Marker prefix used to flag a token as a Bearer JWT (e.g. one minted via
    /// <c>UGS_CLI_AUTH_TOKEN</c>). The prefix is stripped before the header is
    /// emitted; tokens without it default to <c>Basic</c> service-account auth.
    /// </summary>
    public const string BearerTokenSchemePrefix = "Bearer:";

    /// <summary>
    /// Create a ready to send header value for this token.
    /// </summary>
    /// <param name="token">
    /// The token to convert.
    /// </param>
    public static string ToHeaderValue(this string token)
    {
        if (token?.StartsWith(BearerTokenSchemePrefix, StringComparison.Ordinal) == true)
        {
            return $"Bearer {token.Substring(BearerTokenSchemePrefix.Length)}";
        }

        return $"Basic {token}";
    }

    /// <summary>
    /// Set the header of the provided token in this header collection.
    /// </summary>
    /// <param name="self">
    /// The header collection to add the header to.
    /// </param>
    /// <param name="token">
    /// The token to add to the header collection.
    /// </param>
    /// <returns>
    /// Returns the header collection for fluent interface.
    /// </returns>
    public static IDictionary<string, string> SetAccessTokenHeader(
        this IDictionary<string, string> self, string token)
    {
        self[HeaderKey] = token.ToHeaderValue();
        return self;
    }
}
