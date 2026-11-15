using System.Text.RegularExpressions;
using Unity.Services.Cli.Common.Exceptions;

namespace Unity.Services.Cli.Player.IdProvider.Registry;

/// <summary>
/// An identity provider type that Unity Authentication supports, and the fields configuring it takes.
/// </summary>
public record IdProviderType(
    string Type,
    string DisplayName,
    bool NeedsClientId,
    bool NeedsClientSecret,
    bool SupportsAdditionalAppIds,
    bool CanBeDeleted,
    string ClientIdLabel,
    string ClientSecretLabel)
{
    /// <summary>
    /// False for the Unity Player Accounts type, which the Player Accounts backend adds and points at its client.
    /// </summary>
    public bool CanBeCreated => Type != IdProviderTypes.PlayerAccountsType;
}

/// <summary>
/// A provider argument resolved to the type the admin API addresses.
/// </summary>
/// <param name="Type">The type to send, e.g. <c>steampowered.com</c> or <c>oidc-mygame</c>.</param>
/// <param name="Definition">The supported type it belongs to, or a placeholder when this CLI does not know the type.</param>
/// <param name="IsKnown">False for a type this CLI does not know, which is sent as passed for the API to validate.</param>
public record ResolvedIdProvider(string Type, IdProviderType Definition, bool IsKnown = true)
{
    public bool IsOpenIdConnect => Definition.Type == IdProviderTypes.OpenIdConnectPrefix;
}

/// <summary>
/// The identity provider types Unity Authentication supports, and resolution of the names a user may pass.
/// </summary>
public static class IdProviderTypes
{
    public const string OpenIdConnectPrefix = "oidc-";
    public const string PlayerAccountsType = "unity";

    /// <summary>
    /// Where a project's Unity Player Accounts sign-in is set up, for messages about the <c>unity</c> provider.
    /// </summary>
    public const string PlayerAccountsSetup =
        "Set up Unity Player Accounts in the Dashboard (Authentication > Identity providers > Unity Player Accounts) "
        + "or with the Unity Player Accounts commands; that creates the project's Player Accounts client, and the "
        + "Player Accounts backend adds and maintains this provider for it.";
    public const string ListTypesCommand = "ugs player id-provider list-types";

    const string k_ClientId = "Client ID";
    const string k_ClientSecret = "Client Secret";

    static readonly Regex k_OpenIdConnectName = new("^oidc-[a-z0-9\\-_\\.]{1,15}$");
    static readonly Regex k_TypeName = new("^[a-z0-9][a-z0-9\\-_\\.]{0,63}$");

    public static IReadOnlyList<IdProviderType> All { get; } = new[]
    {
        new IdProviderType("apple.com", "Sign-in with Apple", true, false, false, true, "App ID", k_ClientSecret),
        new IdProviderType("apple-game-center", "Apple Game Center", true, false, false, true, "Bundle ID", k_ClientSecret),
        new IdProviderType("code-link", "Code-Link", false, false, false, true, k_ClientId, k_ClientSecret),
        new IdProviderType("custom", "Custom ID", false, false, false, true, k_ClientId, k_ClientSecret),
        new IdProviderType("facebook.com", "Facebook", true, true, false, true, "App ID", "App Secret"),
        new IdProviderType("google.com", "Google", true, false, false, true, k_ClientId, k_ClientSecret),
        new IdProviderType("google-play-games", "Google Play Games", true, true, false, true, k_ClientId, k_ClientSecret),
        new IdProviderType("oculus", "Oculus", true, true, false, true, "App ID", "App Secret"),
        new IdProviderType(OpenIdConnectPrefix, "OpenID Connect", true, false, false, true, k_ClientId, k_ClientSecret),
        new IdProviderType("steampowered.com", "Steam", true, true, true, true, "App ID", "Key"),
        new IdProviderType(PlayerAccountsType, "Unity Player Accounts", true, false, false, true, k_ClientId, k_ClientSecret),
        new IdProviderType("usernamepassword", "Username and Password", false, false, false, false, k_ClientId, k_ClientSecret),
    };

    /// <summary>
    /// The supported type a type string returned by the admin API belongs to, or null when it is not one this
    /// CLI knows.
    /// </summary>
    public static IdProviderType? Find(string? type)
    {
        if (string.IsNullOrEmpty(type))
        {
            return null;
        }

        if (type.StartsWith(OpenIdConnectPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return All.First(definition => definition.Type == OpenIdConnectPrefix);
        }

        return All.FirstOrDefault(definition => string.Equals(definition.Type, type, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Resolve a provider argument: a type (<c>steampowered.com</c>, <c>oidc-mygame</c>), a display name
    /// (<c>Steam</c>), or a short alias (<c>steam</c>, <c>apple</c>).
    /// </summary>
    /// <exception cref="CliException">The argument is empty, unknown, ambiguous, or an invalid OpenID Connect name.</exception>
    public static ResolvedIdProvider Resolve(string? provider)
    {
        if (string.IsNullOrWhiteSpace(provider))
        {
            throw new CliException(
                $"A provider is required. Run `{ListTypesCommand}` to see the accepted values.",
                ExitCode.HandledError);
        }

        var value = provider.Trim();
        var openIdConnect = All.First(definition => definition.Type == OpenIdConnectPrefix);

        if (value.StartsWith(OpenIdConnectPrefix, StringComparison.OrdinalIgnoreCase))
        {
            if (!k_OpenIdConnectName.IsMatch(value))
            {
                throw new CliException(
                    $"'{value}' is not a valid OpenID Connect provider type. Use '{OpenIdConnectPrefix}<name>', where "
                    + "<name> is 1 to 15 lowercase letters, digits, '-', '_' or '.'.",
                    ExitCode.HandledError);
            }

            return new ResolvedIdProvider(value, openIdConnect);
        }

        var matches = All
            .Where(definition => AliasesOf(definition).Contains(value, StringComparer.OrdinalIgnoreCase))
            .ToList();

        if (matches.Count > 1)
        {
            throw new CliException(
                $"'{provider}' matches several identity providers ({string.Join(", ", matches.Select(match => match.Type))}). "
                + "Pass one of those types.",
                ExitCode.HandledError);
        }

        if (matches.Count == 0)
        {
            if (!k_TypeName.IsMatch(value))
            {
                throw new CliException(
                    $"'{provider}' is not a provider type. Pass a type as the API names it (lowercase letters, digits, "
                    + $"'-', '_' or '.'), or an alias from `{ListTypesCommand}`.",
                    ExitCode.HandledError);
            }

            var unknown = new IdProviderType(value, value, false, false, false, true, "Client ID", "Client Secret");
            return new ResolvedIdProvider(value, unknown, IsKnown: false);
        }

        if (matches[0] == openIdConnect)
        {
            throw new CliException(
                $"OpenID Connect providers are addressed by name: pass '{OpenIdConnectPrefix}<name>'.",
                ExitCode.HandledError);
        }

        return new ResolvedIdProvider(matches[0].Type, matches[0]);
    }

    static IEnumerable<string> AliasesOf(IdProviderType definition)
    {
        var type = definition.Type;
        yield return type;
        yield return type.Replace("-", string.Empty);

        var domain = type.IndexOf('.');
        if (domain > 0)
        {
            yield return type.Substring(0, domain);
        }

        yield return definition.DisplayName;
        yield return new string(definition.DisplayName.Where(char.IsLetterOrDigit).ToArray());
    }
}
