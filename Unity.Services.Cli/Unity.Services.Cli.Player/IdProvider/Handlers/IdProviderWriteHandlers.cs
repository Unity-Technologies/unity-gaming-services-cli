using Microsoft.Extensions.Logging;
using Unity.Services.Cli.Common.Console;
using Unity.Services.Cli.Common.Exceptions;
using Unity.Services.Cli.Common.Logging;
using Unity.Services.Cli.Player.IdProvider.Input;
using Unity.Services.Cli.Player.IdProvider.Models;
using Unity.Services.Cli.Player.IdProvider.Registry;
using Unity.Services.Cli.Player.IdProvider.Service;

namespace Unity.Services.Cli.Player.IdProvider.Handlers;

public static class IdProviderCreateHandler
{
    public static async Task CreateAsync(IdProviderInput input, IIdProviderService service,
        IClientSecretInput secretInput, ILogger logger, ILoadingIndicator loadingIndicator,
        CancellationToken cancellationToken)
    {
        var target = IdProviderTypes.Resolve(input.Provider);
        if (!target.Definition.CanBeCreated)
        {
            throw new CliException(
                $"'{target.Type}' cannot be created directly. {IdProviderTypes.PlayerAccountsSetup}",
                ExitCode.HandledError);
        }

        if (input.ClearAdditionalAppIds)
        {
            throw new CliException("--clear-additional-app-ids applies to update only.", ExitCode.HandledError);
        }

        var appIds = IdProviderFields.ParseAppIds(input.AdditionalAppIds);
        var definition = target.Definition;
        if (definition.NeedsClientSecret && !input.ClientSecretFromStdin)
        {
            throw new CliException(
                $"'{target.Type}' requires a client secret ({definition.ClientSecretLabel}): pipe it in with "
                + $"{IdProviderInput.ClientSecretStdinAlias}.",
                ExitCode.HandledError);
        }

        IdProviderFields.ValidateForType(target.Type, target.IsOpenIdConnect, target.IsKnown ? definition : null,
            input.Issuer, input.DisplayName, appIds);
        if (!target.IsKnown)
        {
            logger.LogWarning(
                "'{Type}' is not a provider type this CLI knows; it is sent as passed and the server validates it.",
                target.Type);
        }

        var secret = input.ClientSecretFromStdin
            ? await ClientSecretReader.ReadAsync(secretInput, "create", cancellationToken)
            : null;

        await loadingIndicator.StartLoadingAsync("Adding identity provider ...", _ =>
            CreateAsync(input, target, secret, appIds, service, logger, cancellationToken));
    }

    internal static async Task CreateAsync(IdProviderInput input, ResolvedIdProvider target, string? secret,
        List<AdditionalAppId>? appIds, IIdProviderService service, ILogger logger, CancellationToken cancellationToken)
    {
        var request = new CreateIdProviderRequest
        {
            Type = target.Type,
            ClientId = input.ClientId,
            ClientSecret = secret,
            DisplayName = input.DisplayName,
            RelyingParty = input.RelyingParty,
            OidcConfig = input.Issuer is null ? null : new OpenIdConfig { Issuer = input.Issuer },
            SteamConfig = appIds is null ? null : new SteamProviderConfig { AdditionalAppIds = appIds },
        };

        var response = await service.CreateAsync(input.CloudProjectId!, request, cancellationToken);
        logger.LogResultValue(IdProviderResult.From(response));
    }
}

public static class IdProviderUpdateHandler
{
    public static async Task UpdateAsync(IdProviderInput input, IIdProviderService service,
        IClientSecretInput secretInput, ILogger logger, ILoadingIndicator loadingIndicator,
        CancellationToken cancellationToken)
    {
        if (input.AdditionalAppIds is { Count: > 0 } && input.ClearAdditionalAppIds)
        {
            throw new CliException(
                "Pass either --additional-app-id or --clear-additional-app-ids, not both.",
                ExitCode.HandledError);
        }

        if (input.ClientId is null && !input.ClientSecretFromStdin && input.DisplayName is null
            && input.RelyingParty is null && input.Issuer is null && input.AdditionalAppIds is not { Count: > 0 }
            && !input.ClearAdditionalAppIds)
        {
            throw new CliException(
                $"Nothing to update: pass --client-id, {IdProviderInput.ClientSecretStdinAlias}, --display-name, "
                + "--relying-party, --issuer, --additional-app-id or --clear-additional-app-ids.",
                ExitCode.HandledError);
        }

        var appIds = input.ClearAdditionalAppIds
            ? new List<AdditionalAppId>()
            : IdProviderFields.ParseAppIds(input.AdditionalAppIds);
        var secret = input.ClientSecretFromStdin
            ? await ClientSecretReader.ReadAsync(secretInput, "update", cancellationToken)
            : null;

        await loadingIndicator.StartLoadingAsync("Updating identity provider ...", _ =>
            UpdateAsync(input, secret, appIds, service, logger, cancellationToken));
    }

    internal static async Task UpdateAsync(IdProviderInput input, string? secret, List<AdditionalAppId>? appIds,
        IIdProviderService service, ILogger logger, CancellationToken cancellationToken)
    {
        var projectId = input.CloudProjectId!;
        var stored = await IdProviderLookup.ResolveAsync(service, projectId, input.Config, cancellationToken);
        var type = stored.Type ?? "";
        var isOpenIdConnect = type.StartsWith(IdProviderTypes.OpenIdConnectPrefix, StringComparison.OrdinalIgnoreCase);
        var definition = IdProviderTypes.Find(type);
        IdProviderFields.ValidateForType(type, isOpenIdConnect, definition, input.Issuer, input.DisplayName, appIds);
        var configId = stored.ConfigId!;
        if (definition is { CanBeCreated: false } && input.ClientId is not null && input.ClientId != stored.ClientId)
        {
            if (!input.DryRun && !input.UseForce)
            {
                throw new CliException(
                    $"The Player Accounts backend maintains '{type}', and changing its client ID points Player Accounts "
                    + "sign-in away from the project's Player Accounts client. Pass --force to change it, or --dry-run "
                    + "to preview.",
                    ExitCode.HandledError);
            }

            logger.LogWarning(
                "The Player Accounts backend maintains '{Type}': changing its client ID points Player Accounts sign-in "
                + "away from the project's Player Accounts client.",
                type);
        }

        var request = new UpdateIdProviderRequest
        {
            ClientId = input.ClientId,
            ClientSecret = secret,
            DisplayName = input.DisplayName,
            RelyingParty = input.RelyingParty,
            OidcConfig = input.Issuer is null ? null : new OpenIdConfig { Issuer = input.Issuer },
            SteamConfig = appIds is null ? null : new SteamProviderConfig { AdditionalAppIds = appIds },
        };

        var changed = IdProviderFields.Changed(stored, request);
        if (input.DryRun)
        {
            logger.LogResultValue(new IdProviderChangeResult
            {
                Status = IdProviderChangeResult.DryRun,
                ConfigId = configId,
                Type = type,
                Message = changed.Count == 0
                    ? $"Configuration {configId} ('{type}') would not change."
                    : $"Would write {string.Join(", ", changed)} on configuration {configId} ('{type}').",
                Provider = IdProviderResult.From(stored),
            });
            return;
        }

        var response = await service.UpdateAsync(projectId, configId, request, cancellationToken);
        logger.LogResultValue(new IdProviderChangeResult
        {
            Status = IdProviderChangeResult.Applied,
            ConfigId = configId,
            Type = type,
            Message = changed.Count == 0
                ? $"Configuration {configId} ('{type}') already had these values."
                : $"Wrote {string.Join(", ", changed)} on configuration {configId} ('{type}').",
            Provider = IdProviderResult.From(response),
        });
    }
}

static class IdProviderFields
{
    const int k_MaxDisplayNameLength = 64;

    /// <summary>
    /// Checks the API does not make: it accepts additional app ids on any type and stores them, and drops an issuer
    /// sent to a type that is not OpenID Connect. Rules the API enforces with a clear error (a missing client ID or
    /// issuer, a display name on a type that takes none) are left to it.
    /// </summary>
    /// <exception cref="CliException">A field is set that the provider type does not take.</exception>
    public static void ValidateForType(string type, bool isOpenIdConnect, IdProviderType? definition,
        string? issuer, string? displayName, IReadOnlyCollection<AdditionalAppId>? appIds)
    {
        if (!isOpenIdConnect && issuer is not null)
        {
            throw new CliException(
                $"--issuer applies to OpenID Connect providers only; '{type}' does not take one.", ExitCode.HandledError);
        }

        if (displayName is not null && displayName.Length > k_MaxDisplayNameLength)
        {
            throw new CliException(
                $"--display-name is at most {k_MaxDisplayNameLength} characters.", ExitCode.HandledError);
        }

        if (appIds is not null && definition?.SupportsAdditionalAppIds != true)
        {
            var option = appIds.Count == 0 ? "--clear-additional-app-ids" : "--additional-app-id";
            throw new CliException($"'{type}' does not take {option}.", ExitCode.HandledError);
        }
    }

    /// <summary>
    /// Parse <c>appId=Description</c> entries; null when none were passed.
    /// </summary>
    /// <exception cref="CliException">An entry has no app id, or a description that is not letters and digits.</exception>
    public static List<AdditionalAppId>? ParseAppIds(IReadOnlyCollection<string>? entries)
    {
        if (entries is not { Count: > 0 })
        {
            return null;
        }

        return entries.Select(ParseAppId).ToList();
    }

    // The admin API rejects a missing description, and any description holding more than letters and digits,
    // with a bare INVALID_PARAMETERS.
    static AdditionalAppId ParseAppId(string entry)
    {
        var separator = entry.IndexOf('=');
        var appId = separator < 0 ? entry.Trim() : entry.Substring(0, separator).Trim();
        var description = separator < 0 ? "" : entry.Substring(separator + 1).Trim();
        if (appId.Length == 0 || description.Length == 0 || !description.All(char.IsAsciiLetterOrDigit))
        {
            throw new CliException(
                $"'{entry}' is not a valid --additional-app-id. Write it as 'appId=Description', where the "
                + "description is letters and digits only, for example 480=Demo.",
                ExitCode.HandledError);
        }

        return new AdditionalAppId { AppId = appId, Description = description };
    }

    /// <summary>
    /// The option names of the fields the update would change. A piped secret always counts: the stored one
    /// cannot be read to compare.
    /// </summary>
    public static List<string> Changed(IdProviderResponse stored, UpdateIdProviderRequest update)
    {
        var changed = new List<string>();
        if (update.ClientId is not null && update.ClientId != stored.ClientId)
        {
            changed.Add("client-id");
        }

        if (update.ClientSecret is not null)
        {
            changed.Add("client-secret");
        }

        if (update.DisplayName is not null && update.DisplayName != stored.DisplayName)
        {
            changed.Add("display-name");
        }

        if (update.RelyingParty is not null && update.RelyingParty != stored.RelyingParty)
        {
            changed.Add("relying-party");
        }

        if (update.OidcConfig is not null && update.OidcConfig.Issuer != stored.OidcConfig?.Issuer)
        {
            changed.Add("issuer");
        }

        if (update.SteamConfig is not null
            && !SameAppIds(stored.SteamConfig?.AdditionalAppIds, update.SteamConfig.AdditionalAppIds))
        {
            changed.Add("additional-app-ids");
        }

        return changed;
    }

    static bool SameAppIds(IReadOnlyList<AdditionalAppId>? stored, IReadOnlyList<AdditionalAppId>? update)
    {
        stored ??= Array.Empty<AdditionalAppId>();
        update ??= Array.Empty<AdditionalAppId>();
        return stored.Count == update.Count
            && stored.Zip(update, (a, b) => a.AppId == b.AppId && a.Description == b.Description).All(same => same);
    }
}
