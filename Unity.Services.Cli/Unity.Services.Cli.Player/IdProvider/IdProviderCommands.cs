using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Unity.Services.Cli.Common;
using Unity.Services.Cli.Common.Console;
using Unity.Services.Cli.Common.Input;
using Unity.Services.Cli.Common.Networking;
using Unity.Services.Cli.Player.IdProvider.Handlers;
using Unity.Services.Cli.Player.IdProvider.Input;
using Unity.Services.Cli.Player.IdProvider.Service;
using Unity.Services.Gateway.PlayerIdentityApiV2.Generated.Api;
using Unity.Services.Gateway.PlayerIdentityApiV2.Generated.Client;

namespace Unity.Services.Cli.Player.IdProvider;

/// <summary>
/// The <c>ugs player id-provider</c> command group: a project's Unity Authentication identity providers.
/// </summary>
public static class IdProviderCommands
{
    public static Command Build()
    {
        var listTypes = new Command("list-types", new CommandDescription(
                "List the identity provider types Unity Authentication supports and the fields each one needs. Does not contact the services gateway.")
            .WithReturn("JSON array of provider types with their required fields and whether they can be created or deleted here.")
            .Build());
        listTypes.SetHandler<IdProviderInput, IIdProviderService, ILogger, ILoadingIndicator, CancellationToken>(
            IdProviderListTypesHandler.ListTypesAsync);

        var list = new Command("list", new CommandDescription("List the identity provider configurations on the project, with their configIds.")
            .WithReturn("JSON array of configurations. Client secrets are never returned; hasClientSecret reports whether one is stored.")
            .Build())
        {
            CommonInput.CloudProjectIdOption,
            IdProviderInput.TypeFilterOption,
        };
        list.SetHandler<IdProviderInput, IIdProviderService, ILogger, ILoadingIndicator, CancellationToken>(
            IdProviderListHandler.ListAsync);

        var get = new Command("get", new CommandDescription("Get one identity provider configuration on the project.")
            .WithReturn("JSON configuration. The client secret is never returned; hasClientSecret reports whether one is stored.")
            .Build())
        {
            CommonInput.CloudProjectIdOption,
            IdProviderInput.ConfigArgument,
        };
        get.SetHandler<IdProviderInput, IIdProviderService, ILogger, ILoadingIndicator, CancellationToken>(
            IdProviderGetHandler.GetAsync);

        var create = new Command("create", new CommandDescription(
                "Add an identity provider to the project. A client secret is read only from standard input, with "
                + $"{IdProviderInput.ClientSecretStdinAlias}.")
            .WithReturn("JSON configuration as added, with its configId. The client secret is never returned.")
            .Build())
        {
            CommonInput.CloudProjectIdOption,
            IdProviderInput.ClientIdOption,
            IdProviderInput.ClientSecretStdinOption,
            IdProviderInput.DisplayNameOption,
            IdProviderInput.RelyingPartyOption,
            IdProviderInput.IssuerOption,
            IdProviderInput.AdditionalAppIdOption,
            IdProviderInput.ProviderArgument,
        };
        create.SetHandler<IdProviderInput, IIdProviderService, IClientSecretInput, ILogger, ILoadingIndicator, CancellationToken>(
            IdProviderCreateHandler.CreateAsync);

        var update = new Command("update", new CommandDescription(
                "Change an identity provider configuration. Only the fields passed are written; the rest keep their stored "
                + "values. --dry-run previews which fields would change.")
            .WithReturn("JSON with the status (applied or dry-run), the configId, a message naming the changed fields, and the configuration.")
            .Build())
        {
            CommonInput.CloudProjectIdOption,
            CommonInput.UseForceOption,
            IdProviderInput.DryRunOption,
            IdProviderInput.ClientIdOption,
            IdProviderInput.ClientSecretStdinOption,
            IdProviderInput.DisplayNameOption,
            IdProviderInput.RelyingPartyOption,
            IdProviderInput.IssuerOption,
            IdProviderInput.AdditionalAppIdOption,
            IdProviderInput.ClearAdditionalAppIdsOption,
            IdProviderInput.ConfigArgument,
        };
        update.SetHandler<IdProviderInput, IIdProviderService, IClientSecretInput, ILogger, ILoadingIndicator, CancellationToken>(
            IdProviderUpdateHandler.UpdateAsync);

        var enable = new Command("enable", new CommandDescription("Enable an identity provider configuration on the project.")
            .WithReturn("JSON configuration after the change.")
            .Build())
        {
            CommonInput.CloudProjectIdOption,
            IdProviderInput.ConfigArgument,
        };
        enable.SetHandler<IdProviderInput, IIdProviderService, ILogger, ILoadingIndicator, CancellationToken>(
            IdProviderEnableHandler.EnableAsync);

        var disable = new Command("disable", new CommandDescription(
                "Disable an identity provider configuration on the project. It is kept; players can no longer sign in with it.")
            .WithReturn("JSON configuration after the change.")
            .Build())
        {
            CommonInput.CloudProjectIdOption,
            IdProviderInput.ConfigArgument,
        };
        disable.SetHandler<IdProviderInput, IIdProviderService, ILogger, ILoadingIndicator, CancellationToken>(
            IdProviderDisableHandler.DisableAsync);

        var delete = new Command("delete", new CommandDescription(
                "Delete an identity provider configuration from the project. Cannot be undone: requires --force; --dry-run previews.")
            .WithReturn("JSON with the status (applied or dry-run), the configId, the provider type and a message.")
            .Build())
        {
            CommonInput.CloudProjectIdOption,
            CommonInput.UseForceOption,
            IdProviderInput.DryRunOption,
            IdProviderInput.ConfigArgument,
        };
        delete.SetHandler<IdProviderInput, IIdProviderService, ILogger, ILoadingIndicator, CancellationToken>(
            IdProviderDeleteHandler.DeleteAsync);

        return new Command("id-provider", new CommandDescription(
                "Manage the identity providers players sign in with in Unity Authentication. These are Authentication's sign-in "
                + "methods, not the game-owned Apple, Google or Facebook apps that Unity Player Accounts signs players in through.")
            .WithDocs("https://docs.unity.com/ugs/manual/authentication/manual")
            .Build())
        {
            listTypes,
            list,
            get,
            create,
            update,
            enable,
            disable,
            delete,
        };
    }

    public static void RegisterServices(IServiceCollection serviceCollection)
    {
        serviceCollection.AddSingleton<IClientSecretInput, ConsoleClientSecretInput>();
        var config = new Configuration
        {
            BasePath = $"{EndpointHelper.GetCurrentEndpointFor<PlayerIdentityEndpoints>()}/api/player-identity/v2",
            UserAgent = RequestHeaderHelper.UserAgent,
        };
        config.DefaultHeaders.SetXClientIdHeader();
        serviceCollection.AddSingleton<IAuthenticationProvidersApiAsync>(new AuthenticationProvidersApi(config));
        serviceCollection.AddTransient<IIdProviderService, IdProviderService>();
    }
}
