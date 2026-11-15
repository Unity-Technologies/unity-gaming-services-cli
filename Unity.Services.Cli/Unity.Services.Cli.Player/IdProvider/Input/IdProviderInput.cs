using System.CommandLine;
using Unity.Services.Cli.Common.Input;
using Unity.Services.Cli.Player.IdProvider.Registry;

namespace Unity.Services.Cli.Player.IdProvider.Input;

public class IdProviderInput : CommonInput
{
    public const string ClientSecretStdinAlias = "--client-secret-stdin";

    public static readonly Argument<string> ProviderArgument = new(
        "provider",
        "Identity provider type: a type (steampowered.com, apple.com, oidc-<name>), a display name (Steam), or a "
        + $"short alias (steam, apple, google). `{IdProviderTypes.ListTypesCommand}` shows the types this CLI knows; "
        + "any other type is sent as passed and the server validates it.");

    public static readonly Argument<string> ConfigArgument = new(
        "config",
        "The configuration: its configId, or a provider type or alias (steam, google, oidc-<name>) when the project "
        + "has exactly one configuration of that type. Run `ugs player id-provider list` to see the configIds.");

    public static readonly Option<bool> DryRunOption = new(
        "--dry-run",
        "Report what would change without changing anything")
    {
        Arity = ArgumentArity.Zero,
    };

    public static readonly Option<string> TypeFilterOption = new(
        "--type",
        "Only list configurations of this provider type or alias");

    public static readonly Option<string> ClientIdOption = new(
        "--client-id",
        "Client ID, App ID or Bundle ID, depending on the provider (see list-types for its label)");

    public static readonly Option<bool> ClientSecretStdinOption = new(
        ClientSecretStdinAlias,
        "Read the client secret, App secret or Steam key from standard input. The only way to pass one, so it "
        + "never appears in the command line or shell history")
    {
        Arity = ArgumentArity.Zero,
    };

    public static readonly Option<string> DisplayNameOption = new(
        "--display-name",
        "OpenID Connect and Custom ID providers only: the name shown to players, up to 64 characters");

    public static readonly Option<string> RelyingPartyOption = new(
        "--relying-party",
        "Relying party, for the providers that take one");

    public static readonly Option<string> IssuerOption = new(
        "--issuer",
        "Issuer URL. OpenID Connect providers (oidc-<name>) only, and required for them");

    public static readonly Option<List<string>> AdditionalAppIdOption = new(
        "--additional-app-id",
        "Steam only: an extra app id, written as 'appId=Description'. Can be supplied more than once; on update, "
        + "replaces the stored list");

    public static readonly Option<bool> ClearAdditionalAppIdsOption = new(
        "--clear-additional-app-ids",
        "Steam only: remove every stored additional app id")
    {
        Arity = ArgumentArity.Zero,
    };

    [InputBinding(nameof(ProviderArgument))]
    public string? Provider { get; set; }

    [InputBinding(nameof(ConfigArgument))]
    public string? Config { get; set; }

    [InputBinding(nameof(DryRunOption))]
    public bool DryRun { get; set; }

    [InputBinding(nameof(TypeFilterOption))]
    public string? TypeFilter { get; set; }

    [InputBinding(nameof(ClientIdOption))]
    public string? ClientId { get; set; }

    [InputBinding(nameof(ClientSecretStdinOption))]
    public bool ClientSecretFromStdin { get; set; }

    [InputBinding(nameof(DisplayNameOption))]
    public string? DisplayName { get; set; }

    [InputBinding(nameof(RelyingPartyOption))]
    public string? RelyingParty { get; set; }

    [InputBinding(nameof(IssuerOption))]
    public string? Issuer { get; set; }

    [InputBinding(nameof(AdditionalAppIdOption))]
    public List<string>? AdditionalAppIds { get; set; }

    [InputBinding(nameof(ClearAdditionalAppIdsOption))]
    public bool ClearAdditionalAppIds { get; set; }
}
