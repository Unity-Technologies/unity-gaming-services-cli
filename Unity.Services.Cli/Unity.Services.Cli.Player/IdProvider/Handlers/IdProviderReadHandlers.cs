using Microsoft.Extensions.Logging;
using Unity.Services.Cli.Common.Console;
using Unity.Services.Cli.Common.Exceptions;
using Unity.Services.Cli.Common.Logging;
using Unity.Services.Cli.Player.IdProvider.Input;
using Unity.Services.Cli.Player.IdProvider.Models;
using Unity.Services.Cli.Player.IdProvider.Registry;
using Unity.Services.Cli.Player.IdProvider.Service;

namespace Unity.Services.Cli.Player.IdProvider.Handlers;

public static class IdProviderListTypesHandler
{
    public static Task ListTypesAsync(IdProviderInput input, IIdProviderService service, ILogger logger,
        ILoadingIndicator loadingIndicator, CancellationToken cancellationToken)
    {
        logger.LogResultValue(new IdProviderTypeResultList(IdProviderTypes.All
            .OrderBy(definition => definition.Type, StringComparer.Ordinal)
            .Select(IdProviderTypeResult.From)));
        return Task.CompletedTask;
    }
}

public static class IdProviderListHandler
{
    public static async Task ListAsync(IdProviderInput input, IIdProviderService service, ILogger logger,
        ILoadingIndicator loadingIndicator, CancellationToken cancellationToken)
    {
        await loadingIndicator.StartLoadingAsync("Listing identity providers ...", _ =>
            ListAsync(input, service, logger, cancellationToken));
    }

    internal static async Task ListAsync(IdProviderInput input, IIdProviderService service, ILogger logger,
        CancellationToken cancellationToken)
    {
        var filter = input.TypeFilter is null ? null : IdProviderTypes.Resolve(input.TypeFilter);
        if (filter is { IsKnown: false })
        {
            logger.LogWarning(
                "'{Type}' is not a provider type this CLI knows, so only configurations of exactly that type are listed. "
                + "Run `{ListTypes}` to see the types it knows.",
                filter.Type, IdProviderTypes.ListTypesCommand);
        }

        var providers = await service.ListAsync(input.CloudProjectId!, filter?.Type, cancellationToken);
        logger.LogResultValue(new IdProviderResultList(providers.Select(IdProviderResult.From)));
    }
}

public static class IdProviderGetHandler
{
    public static async Task GetAsync(IdProviderInput input, IIdProviderService service, ILogger logger,
        ILoadingIndicator loadingIndicator, CancellationToken cancellationToken)
    {
        await loadingIndicator.StartLoadingAsync("Getting identity provider ...", _ =>
            GetAsync(input, service, logger, cancellationToken));
    }

    internal static async Task GetAsync(IdProviderInput input, IIdProviderService service, ILogger logger,
        CancellationToken cancellationToken)
    {
        var config = await IdProviderLookup.ResolveAsync(service, input.CloudProjectId!, input.Config, cancellationToken);
        logger.LogResultValue(IdProviderResult.From(config));
    }
}

static class IdProviderLookup
{
    const string k_ListCommand = "ugs player id-provider list";

    /// <summary>
    /// Resolve a configuration argument: a configId is fetched directly; a provider type or alias must match
    /// exactly one configuration on the project.
    /// </summary>
    /// <exception cref="CliException">The argument is empty, or the type matches no or several configurations.</exception>
    public static async Task<IdProviderResponse> ResolveAsync(IIdProviderService service, string projectId,
        string? config, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(config))
        {
            throw new CliException(
                $"A configuration is required: pass its configId, or a provider type. Run `{k_ListCommand}` to see them.",
                ExitCode.HandledError);
        }

        var value = config.Trim();
        if (Guid.TryParse(value, out _))
        {
            return await service.GetAsync(projectId, value, cancellationToken);
        }

        var resolved = IdProviderTypes.Resolve(value);
        var type = resolved.Type;
        var matches = (await service.ListAsync(projectId, type, cancellationToken))
            .Where(candidate => string.Equals(candidate.Type, type, StringComparison.OrdinalIgnoreCase))
            .ToList();

        return matches.Count switch
        {
            1 => matches[0],
            0 => throw new CliException(
                $"No '{type}' configuration on project {projectId}. Run `{k_ListCommand}` to see what is configured"
                + (resolved.IsKnown ? "." : $"; '{type}' is not a type this CLI knows, see `{IdProviderTypes.ListTypesCommand}`."),
                ExitCode.HandledError),
            _ => throw new CliException(
                $"'{type}' has {matches.Count} configurations on project {projectId}: "
                + $"{string.Join(", ", matches.Select(match => match.ConfigId))}. Pass one of those configIds.",
                ExitCode.HandledError),
        };
    }
}
