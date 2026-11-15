using Microsoft.Extensions.Logging;
using Unity.Services.Cli.Common.Console;
using Unity.Services.Cli.Common.Exceptions;
using Unity.Services.Cli.Common.Logging;
using Unity.Services.Cli.Player.IdProvider.Input;
using Unity.Services.Cli.Player.IdProvider.Models;
using Unity.Services.Cli.Player.IdProvider.Registry;
using Unity.Services.Cli.Player.IdProvider.Service;

namespace Unity.Services.Cli.Player.IdProvider.Handlers;

public static class IdProviderEnableHandler
{
    public static async Task EnableAsync(IdProviderInput input, IIdProviderService service, ILogger logger,
        ILoadingIndicator loadingIndicator, CancellationToken cancellationToken)
    {
        await loadingIndicator.StartLoadingAsync("Enabling identity provider ...", _ =>
            EnableAsync(input, service, logger, cancellationToken));
    }

    internal static async Task EnableAsync(IdProviderInput input, IIdProviderService service, ILogger logger,
        CancellationToken cancellationToken)
    {
        var projectId = input.CloudProjectId!;
        var config = await IdProviderLookup.ResolveAsync(service, projectId, input.Config, cancellationToken);
        var response = await service.EnableAsync(projectId, config.ConfigId!, cancellationToken);
        logger.LogResultValue(IdProviderResult.From(response));
    }
}

public static class IdProviderDisableHandler
{
    public static async Task DisableAsync(IdProviderInput input, IIdProviderService service, ILogger logger,
        ILoadingIndicator loadingIndicator, CancellationToken cancellationToken)
    {
        await loadingIndicator.StartLoadingAsync("Disabling identity provider ...", _ =>
            DisableAsync(input, service, logger, cancellationToken));
    }

    internal static async Task DisableAsync(IdProviderInput input, IIdProviderService service, ILogger logger,
        CancellationToken cancellationToken)
    {
        var projectId = input.CloudProjectId!;
        var config = await IdProviderLookup.ResolveAsync(service, projectId, input.Config, cancellationToken);
        var response = await service.DisableAsync(projectId, config.ConfigId!, cancellationToken);
        logger.LogResultValue(IdProviderResult.From(response));
    }
}

public static class IdProviderDeleteHandler
{
    public static async Task DeleteAsync(IdProviderInput input, IIdProviderService service, ILogger logger,
        ILoadingIndicator loadingIndicator, CancellationToken cancellationToken)
    {
        await loadingIndicator.StartLoadingAsync("Deleting identity provider ...", _ =>
            DeleteAsync(input, service, logger, cancellationToken));
    }

    internal static async Task DeleteAsync(IdProviderInput input, IIdProviderService service, ILogger logger,
        CancellationToken cancellationToken)
    {
        var projectId = input.CloudProjectId!;
        var config = await IdProviderLookup.ResolveAsync(service, projectId, input.Config, cancellationToken);
        var configId = config.ConfigId!;
        if (IdProviderTypes.Find(config.Type)?.CanBeDeleted == false)
        {
            throw new CliException(
                $"'{config.Type}' cannot be deleted. Turn it off with `ugs player id-provider disable {configId}` instead.",
                ExitCode.HandledError);
        }

        if (IdProviderTypes.Find(config.Type) is { CanBeCreated: false })
        {
            logger.LogWarning(
                "The Player Accounts backend maintains '{Type}': deleting it turns off Unity Player Accounts sign-in, "
                + "and the CLI cannot add it back. {Setup}",
                config.Type, IdProviderTypes.PlayerAccountsSetup);
        }

        if (input.DryRun)
        {
            logger.LogResultValue(new IdProviderChangeResult
            {
                Status = IdProviderChangeResult.DryRun,
                ConfigId = configId,
                Type = config.Type ?? "",
                Message = $"Would delete configuration {configId} ('{config.Type}') from project {projectId}.",
                Provider = IdProviderResult.From(config),
            });
            return;
        }

        if (!input.UseForce)
        {
            throw new CliException(
                $"Deleting configuration {configId} ('{config.Type}') stops its players from signing in with it and "
                + "cannot be undone. Pass --force to delete it, or --dry-run to preview.",
                ExitCode.HandledError);
        }

        await service.DeleteAsync(projectId, configId, cancellationToken);
        logger.LogResultValue(new IdProviderChangeResult
        {
            Status = IdProviderChangeResult.Applied,
            ConfigId = configId,
            Type = config.Type ?? "",
            Message = $"Deleted configuration {configId} ('{config.Type}') from project {projectId}.",
        });
    }
}
