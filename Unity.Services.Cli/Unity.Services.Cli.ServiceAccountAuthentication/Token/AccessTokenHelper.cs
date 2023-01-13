namespace Unity.Services.Cli.ServiceAccountAuthentication.Token;

public static class AccessTokenHelper
{
    /// <summary>
    /// The key of the access token used in a request header.
    /// </summary>
    public const string HeaderKey = "Authorization";

    /// <summary>
    /// Create a ready to send header value for this token.
    /// </summary>
    /// <param name="token">
    /// The token to convert.
    /// </param>
    public static string ToHeaderValue(this string token)
        => $"Basic {token}";

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
