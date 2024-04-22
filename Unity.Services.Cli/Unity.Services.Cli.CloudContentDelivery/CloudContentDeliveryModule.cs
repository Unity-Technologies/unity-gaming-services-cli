using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Unity.Services.Cli.CloudContentDelivery.Handlers.Badges;
using Unity.Services.Cli.CloudContentDelivery.Handlers.Buckets;
using Unity.Services.Cli.CloudContentDelivery.Handlers.Entries;
using Unity.Services.Cli.CloudContentDelivery.Handlers.Releases;
using Unity.Services.Cli.CloudContentDelivery.Input;
using Unity.Services.Cli.CloudContentDelivery.IO;
using Unity.Services.Cli.CloudContentDelivery.Service;
using Unity.Services.Cli.Common;
using Unity.Services.Cli.Common.Console;
using Unity.Services.Cli.Common.Input;
using Unity.Services.Cli.Common.Networking;
using Unity.Services.Cli.Common.Utils;
using Unity.Services.Cli.Common.Validator;
using Unity.Services.CloudContentDelivery.Authoring.Core.IO;
using Unity.Services.Gateway.ContentDeliveryManagementApiV1.Generated.Api;
using Unity.Services.Gateway.ContentDeliveryManagementApiV1.Generated.Client;

namespace Unity.Services.Cli.CloudContentDelivery;

/// <summary>
///     A Template module to achieve a get request command: ugs cloudcontentdelivery get `address` -o `file`
/// </summary>
public class CloudContentDeliveryModule : ICommandModule
{
    public CloudContentDeliveryModule()
    {
        ModuleRootCommand = new Command("ccd", "Manage Cloud Content Delivery.");
        RegisterModulesCommands(ModuleRootCommand);
    }

    public Command? ModuleRootCommand { get; }

    /// <summary>
    ///     Register service to UGS CLI host builder
    /// </summary>
    ///
    ///
    public static void RegisterServices(HostBuilderContext hostBuilderContext, IServiceCollection serviceCollection)
    {
        var config = new Configuration
        {
            BasePath = EndpointHelper.GetCurrentEndpointFor<CloudContentDeliveryApiEndpoints>(),
            Timeout = 600000,
            UserAgent = "ugs_cli/1.0.0"
        };
        config.DefaultHeaders.SetXClientIdHeader();

        serviceCollection.AddSingleton<BucketClient, BucketClient>();
        serviceCollection.AddSingleton<BadgeClient, BadgeClient>();
        serviceCollection.AddSingleton<EntryClient, EntryClient>();
        serviceCollection.AddSingleton<ReleaseClient, ReleaseClient>();
        serviceCollection.AddSingleton<IBadgesApi>(new BadgesApi(config));
        serviceCollection.AddSingleton<IBucketsApi>(new BucketsApi(config));
        serviceCollection.AddSingleton<IReleasesApi>(new ReleasesApi(config));
        serviceCollection.AddSingleton<IEntriesApi>(new EntriesApi(config));
        serviceCollection.AddSingleton<IPermissionsApi>(new PermissionsApi(config));
        serviceCollection.AddSingleton<IContentApi>(new ContentApi(config));

        serviceCollection.AddSingleton<ClientWrapper>(serviceProvider => new ClientWrapper(
            serviceProvider.GetRequiredService<ReleaseClient>(),
            serviceProvider.GetRequiredService<BadgeClient>(),
            serviceProvider.GetRequiredService<BucketClient>(),
            serviceProvider.GetRequiredService<EntryClient>()));

        serviceCollection.AddSingleton<SynchronizationService, SynchronizationService>();
        serviceCollection.AddSingleton<IUploadContentClient>(new UploadContentClient(new HttpClient()));
        serviceCollection.AddSingleton<HttpClient>();
        serviceCollection.AddSingleton<IContentDeliveryValidator>(
            new ContentDeliveryValidator(new ConfigurationValidator()));

        serviceCollection.AddTransient<IFileSystem, FileSystem>();

        /*
         This will commented until we implement Deployment/Fetch

        // Registers services required for Deployment/Fetch
        // Register the command handler
        serviceCollection.AddTransient<IDeploymentService, CloudContentDeliveryDeploymentService>();
        serviceCollection.AddTransient<ICloudContentDeliveryDeploymentHandler, CloudContentDeliveryDeploymentHandler>();
        serviceCollection.AddTransient<IFetchService, CloudContentDeliveryFetchService>();
        serviceCollection.AddTransient<ICloudContentDeliveryFetchHandler, CloudContentDeliveryFetchHandler>();*/
    }

    public static void RegisterModulesCommands(Command root)
    {
        var bucketHandlerCommand = CreateBucketHandlerCommand();
        var entryHandlerCommand = CreateEntryHandlerCommand();
        var releaseHandlerCommand = CreateReleaseHandlerCommand();
        var badgeHandlerCommand = CreateBadgeHandlerCommand();

        root.Add(bucketHandlerCommand);
        root.Add(entryHandlerCommand);
        root.Add(releaseHandlerCommand);
        root.Add(badgeHandlerCommand);
    }

    static Command CreateBucketHandlerCommand()
    {
        var listBucketHandlerCommand = new Command(
            "list",
            "List buckets for a project.")
        {
            CommonInput.EnvironmentNameOption,
            CommonInput.CloudProjectIdOption,
            CloudContentDeliveryInput.PageOption,
            CloudContentDeliveryInput.PerPageOption,
            CloudContentDeliveryInput.FilterNameOption,
            CloudContentDeliveryInputBuckets.SortByBucketOption,
            CloudContentDeliveryInput.SortOrderOption
        };

        listBucketHandlerCommand.SetHandler<
            CloudContentDeliveryInputBuckets,
            IUnityEnvironment,
            BucketClient,
            ILogger,
            ILoadingIndicator,
            CancellationToken>(
            ListBucketHandler.ListAsync);

        var createBucketHandlerCommand = new Command(
            "create",
            "Create bucket for a project.")
        {
            CloudContentDeliveryInput.BucketNameArgument,
            CommonInput.EnvironmentNameOption,
            CommonInput.CloudProjectIdOption,
            CloudContentDeliveryInputBuckets.BucketDescriptionOption,
            CloudContentDeliveryInputBuckets.BucketPrivateOption
        };

        createBucketHandlerCommand.SetHandler<
            CloudContentDeliveryInputBuckets,
            IUnityEnvironment,
            BucketClient,
            ILogger,
            CancellationToken>(
            CreateBucketHandler.CreateAsync);

        var deleteBucketHandlerCommand = new Command(
            "delete",
            "Delete buckets.")
        {
            CloudContentDeliveryInput.BucketNameArgument,
            CommonInput.EnvironmentNameOption,
            CommonInput.CloudProjectIdOption
        };

        deleteBucketHandlerCommand.SetHandler<
            CloudContentDeliveryInput,
            IUnityEnvironment,
            BucketClient,
            ILogger,
            ILoadingIndicator,
            CancellationToken>(
            DeleteBucketHandler.DeleteAsync);

        var infoBucketHandlerCommand = new Command(
            "info",
            "Get bucket info.")
        {
            CloudContentDeliveryInput.BucketNameArgument,
            CommonInput.EnvironmentNameOption,
            CommonInput.CloudProjectIdOption
        };

        infoBucketHandlerCommand.SetHandler<
            CloudContentDeliveryInputBuckets,
            IUnityEnvironment,
            BucketClient,
            ILogger,
            ILoadingIndicator,
            CancellationToken>(
            GetBucketHandler.GetAsync);

        var permissionsBucketUpdateHandlerCommand = new Command(
            "update",
            "Manage permissions for a bucket.")
        {
            CloudContentDeliveryInput.BucketNameArgument,
            CommonInput.EnvironmentNameOption,
            CommonInput.CloudProjectIdOption,
            CloudContentDeliveryInputBuckets.ActionOption,
            CloudContentDeliveryInputBuckets.PermissionOption,
            CloudContentDeliveryInputBuckets.RoleOption
        };

        permissionsBucketUpdateHandlerCommand.SetHandler<
            CloudContentDeliveryInputBuckets,
            IUnityEnvironment,
            BucketClient,
            ILogger,
            ILoadingIndicator,
            CancellationToken>(
            PermissionBucketHandler.PermissionUpdateAsync);

        var permissionsBucketHandlerCommand = new Command(
            "permissions",
            "Manage permissions for a bucket.")
        {
            permissionsBucketUpdateHandlerCommand
        };

        var bucketHandlerCommand = new Command(
            "buckets",
            "Manage buckets for a project.")
        {
            listBucketHandlerCommand,
            createBucketHandlerCommand,
            deleteBucketHandlerCommand,
            infoBucketHandlerCommand,
            permissionsBucketHandlerCommand
        };
        return bucketHandlerCommand;
    }

    static Command CreateReleaseHandlerCommand()
    {
        var createReleaseHandlerCommand = new Command(
            "create",
            "Create release from latest version of current bucket.")
        {
            CommonInput.EnvironmentNameOption,
            CommonInput.CloudProjectIdOption,
            CloudContentDeliveryInput.BucketNameOption,
            CloudContentDeliveryInput.NoteOption,
            CloudContentDeliveryInput.ReleaseMetadataOption
        };

        createReleaseHandlerCommand.SetHandler<
            CloudContentDeliveryInput,
            IUnityEnvironment,
            ReleaseClient,
            BucketClient,
            ILogger,
            ILoadingIndicator,
            CancellationToken>(
            CreateReleaseHandler.CreateAsync);

        var infoReleaseHandlerCommand = new Command(
            "info",
            "Get release info for specific release.")
        {
            CommonInput.EnvironmentNameOption,
            CommonInput.CloudProjectIdOption,
            CloudContentDeliveryInput.BucketNameOption,
            CloudContentDeliveryInput.ReleaseNumArgument
        };

        infoReleaseHandlerCommand.SetHandler<
            CloudContentDeliveryInput,
            IUnityEnvironment,
            ReleaseClient,
            BucketClient,
            ILogger,
            ILoadingIndicator,
            CancellationToken>(
            GetReleaseHandler.GetAsync);
        var listReleaseHandlerCommand = new Command(
            "list",
            "List releases for current bucket.")
        {
            CommonInput.EnvironmentNameOption,
            CommonInput.CloudProjectIdOption,
            CloudContentDeliveryInput.BucketNameOption,
            CloudContentDeliveryInput.PageOption,
            CloudContentDeliveryInput.PerPageOption,
            CloudContentDeliveryInput.ReleaseNumOpt,
            CloudContentDeliveryInput.NoteOption,
            CloudContentDeliveryInput.PromotedFromBucketOption,
            CloudContentDeliveryInput.PromotedFromReleaseOption,
            CloudContentDeliveryInput.BadgeOption,
            CloudContentDeliveryInput.SortByReleaseOption,
            CloudContentDeliveryInput.SortOrderOption
        };

        listReleaseHandlerCommand.SetHandler<
            CloudContentDeliveryInput,
            IUnityEnvironment,
            ReleaseClient,
            BucketClient,
            ILogger,
            ILoadingIndicator,
            CancellationToken>(
            ListReleaseHandler.ListAsync);

        var promoteReleaseHandlerCommand = new Command(
            "promote",
            "Promote release to another bucket.")
        {
            CommonInput.EnvironmentNameOption,
            CommonInput.CloudProjectIdOption,
            CloudContentDeliveryInput.BucketNameOption,
            CloudContentDeliveryInput.ReleaseNumArgument,
            CloudContentDeliveryInput.TargetBucketNameArgument,
            CloudContentDeliveryInput.TargetEnvironmentNameArgument,
            CloudContentDeliveryInput.NoteOption
        };

        promoteReleaseHandlerCommand.SetHandler<
            CloudContentDeliveryInputBuckets,
            IUnityEnvironment,
            ReleaseClient,
            BucketClient,
            ILogger,
            ILoadingIndicator,
            CancellationToken>(
            PromoteBucketHandler.PromoteAsync);

        var promotionsStatusReleaseHandlerCommand = new Command(
            "status",
            "Check promotion status.")
        {
            CommonInput.EnvironmentNameOption,
            CommonInput.CloudProjectIdOption,
            CloudContentDeliveryInput.BucketNameOption,
            CloudContentDeliveryInput.PromotionIdArgument
        };
        var promotionsReleaseHandlerCommand = new Command(
            "promotions",
            "Manage promotions.")
        {
            promotionsStatusReleaseHandlerCommand
        };

        promotionsStatusReleaseHandlerCommand.SetHandler<
            CloudContentDeliveryInputBuckets,
            IUnityEnvironment,
            BucketClient,
            ILogger,
            ILoadingIndicator,
            CancellationToken>(
            PromotionBucketHandler.PromotionStatusAsync);

        var updateReleaseHandlerCommand = new Command(
            "update",
            "Update an existing Release.")
        {
            CommonInput.EnvironmentNameOption,
            CommonInput.CloudProjectIdOption,
            CloudContentDeliveryInput.BucketNameOption,
            CloudContentDeliveryInput.ReleaseNumArgument,
            CloudContentDeliveryInput.NoteOptionRequired
        };

        updateReleaseHandlerCommand.SetHandler<
            CloudContentDeliveryInput,
            IUnityEnvironment,
            ReleaseClient,
            BucketClient,
            ILogger,
            ILoadingIndicator,
            CancellationToken>(
            UpdateReleaseHandler.UpdateAsync);

        var releaseHandlerCommand = new Command(
            "releases",
            "Manage releases for current bucket.")
        {
            createReleaseHandlerCommand,
            infoReleaseHandlerCommand,
            listReleaseHandlerCommand,
            promoteReleaseHandlerCommand,
            promotionsReleaseHandlerCommand,
            updateReleaseHandlerCommand
        };
        return releaseHandlerCommand;
    }

    static Command CreateEntryHandlerCommand()
    {
        var copyEntryHandlerCommand = new Command(
            "copy",
            "Create entry for current bucket from a local file.")
        {
            CloudContentDeliveryInput.LocalPathArgument,
            CloudContentDeliveryInput.RemotePathArgument,
            CommonInput.EnvironmentNameOption,
            CommonInput.CloudProjectIdOption,
            CloudContentDeliveryInput.BucketNameOption,
            CloudContentDeliveryInput.LabelsOption,
            CloudContentDeliveryInput.MetadataOption
        };

        copyEntryHandlerCommand.SetHandler<
            CloudContentDeliveryInput,
            IUnityEnvironment,
            EntryClient,
            BucketClient,
            ILogger,
            ILoadingIndicator,
            CancellationToken>(
            CopyEntryHandler.CopyAsync);

        var deleteEntryHandlerCommand = new Command(
            "delete",
            "Delete entry from current bucket.")
        {
            CloudContentDeliveryInput.EntryPathArgument,
            CommonInput.EnvironmentNameOption,
            CommonInput.CloudProjectIdOption,
            CloudContentDeliveryInput.BucketNameOption
        };

        deleteEntryHandlerCommand.SetHandler<
            CloudContentDeliveryInput,
            IUnityEnvironment,
            EntryClient,
            BucketClient,
            ILogger,
            ILoadingIndicator,
            CancellationToken>(
            DeleteEntryHandler.DeleteAsync);

        var downloadEntryHandlerCommand = new Command(
            "download",
            "Download entry content from current bucket.")
        {
            CloudContentDeliveryInput.EntryPathArgument,
            CommonInput.EnvironmentNameOption,
            CommonInput.CloudProjectIdOption,
            CloudContentDeliveryInput.BucketNameOption,
            CloudContentDeliveryInput.VersionIdOption
        };

        downloadEntryHandlerCommand.SetHandler<
            CloudContentDeliveryInput,
            IUnityEnvironment,
            EntryClient,
            BucketClient,
            ILogger,
            ILoadingIndicator,
            CancellationToken>(
            DownloadEntryHandler.DownloadAsync);

        var infoEntryHandlerCommand = new Command(
            "info",
            "Get entry info from current bucket.")
        {
            CloudContentDeliveryInput.EntryPathArgument,
            CommonInput.EnvironmentNameOption,
            CommonInput.CloudProjectIdOption,
            CloudContentDeliveryInput.BucketNameOption,
            CloudContentDeliveryInput.VersionIdOption
        };

        infoEntryHandlerCommand.SetHandler<
            CloudContentDeliveryInput,
            IUnityEnvironment,
            EntryClient,
            BucketClient,
            ILogger,
            ILoadingIndicator,
            CancellationToken>(
            GetEntryHandler.GetAsync);

        var listEntryHandlerCommand = new Command(
            "list",
            "List entries for current bucket.")
        {
            CommonInput.EnvironmentNameOption,
            CommonInput.CloudProjectIdOption,
            CloudContentDeliveryInput.BucketNameOption,
            CloudContentDeliveryInput.PageOption,
            CloudContentDeliveryInput.PerPageOption,
            CloudContentDeliveryInput.SortByEntryOption,
            CloudContentDeliveryInput.SortOrderOption,
            CloudContentDeliveryInput.StartingAfterOption,
            CloudContentDeliveryInput.PathOption,
            CloudContentDeliveryInput.LabelOption,
            CloudContentDeliveryInput.ContentTypeOption,
            CloudContentDeliveryInput.CompleteOption
        };

        listEntryHandlerCommand.SetHandler<
            CloudContentDeliveryInput,
            IUnityEnvironment,
            EntryClient,
            BucketClient,
            ILogger,
            ILoadingIndicator,
            CancellationToken>(
            ListEntryHandler.ListAsync);

        var syncEntryHandlerCommand = new Command(
            "sync",
            "Sync entries from local directory for current bucket.\nAutomatically creates, updates, and deletes entries\nwithin the bucket to match the files in the local directory.")
        {
            CommonInput.EnvironmentNameOption,
            CommonInput.CloudProjectIdOption,
            CloudContentDeliveryInput.BucketNameOption,
            CloudContentDeliveryInput.LocalFolderArgument,
            CloudContentDeliveryInput.ExclusionPatternOption,
            CloudContentDeliveryInput.DryRunOption,
            CloudContentDeliveryInput.RetryOption,
            CloudContentDeliveryInput.TimeoutOption,
            CloudContentDeliveryInput.DeleteOption,
            CloudContentDeliveryInput.LabelsOption,
            CloudContentDeliveryInput.CreateReleaseOption,
            CloudContentDeliveryInput.IncludeSyncEntriesOnlyOption,
            CloudContentDeliveryInput.UpdateBadgeOption,
            CloudContentDeliveryInput.SyncMetadataOption,
            CloudContentDeliveryInput.ReleaseNotesOption,
            CloudContentDeliveryInput.VerboseOption
        };



        syncEntryHandlerCommand.SetHandler<
            CloudContentDeliveryInput,
            IUnityEnvironment,
            ClientWrapper,
            SynchronizationService,
            ILogger,
            ILoadingIndicator,
            CancellationToken>(
            SyncEntryHandler.SyncEntriesAsync);

        var updateEntryHandlerCommand = new Command(
            "update",
            "Update entry for current bucket.")
        {
            CloudContentDeliveryInput.EntryPathArgument,
            CommonInput.EnvironmentNameOption,
            CommonInput.CloudProjectIdOption,
            CloudContentDeliveryInput.BucketNameOption,
            CloudContentDeliveryInput.LabelsOption,
            CloudContentDeliveryInput.MetadataOption,
            CloudContentDeliveryInput.VersionIdOption
        };

        updateEntryHandlerCommand.SetHandler<
            CloudContentDeliveryInput,
            IUnityEnvironment,
            EntryClient,
            BucketClient,
            ILogger,
            ILoadingIndicator,
            CancellationToken>(
            UpdateEntryHandler.UpdateAsync);

        var entryHandlerCommand = new Command(
            "entries",
            "Manage entries for current bucket.")
        {
            copyEntryHandlerCommand,
            deleteEntryHandlerCommand,
            downloadEntryHandlerCommand,
            infoEntryHandlerCommand,
            listEntryHandlerCommand,
            syncEntryHandlerCommand,
            updateEntryHandlerCommand
        };
        return entryHandlerCommand;
    }

    static Command CreateBadgeHandlerCommand()
    {
        var createBadgeHandlerCommand = new Command(
            "create",
            "Create a new badge or move an existing one")
        {
            CloudContentDeliveryInput.ReleaseNumArgument,
            CloudContentDeliveryInput.BadgeNameArgument,
            CommonInput.EnvironmentNameOption,
            CommonInput.CloudProjectIdOption,
            CloudContentDeliveryInput.BucketNameOption
        };

        createBadgeHandlerCommand.SetHandler<
            CloudContentDeliveryInput,
            IUnityEnvironment,
            BadgeClient,
            BucketClient,
            ILogger,
            ILoadingIndicator,
            CancellationToken>(
            CreateBadgeHandler.CreateAsync);

        var listBadgeHandlerCommand = new Command(
            "list",
            "List badges in the current bucket.")
        {
            CommonInput.EnvironmentNameOption,
            CommonInput.CloudProjectIdOption,
            CloudContentDeliveryInput.BucketNameOption,
            CloudContentDeliveryInput.PageOption,
            CloudContentDeliveryInput.PerPageOption,
            CloudContentDeliveryInput.FilterNameOption,
            CloudContentDeliveryInput.SortByBadgeOption,
            CloudContentDeliveryInput.SortOrderOption
        };

        listBadgeHandlerCommand.SetHandler<
            CloudContentDeliveryInput,
            IUnityEnvironment,
            BadgeClient,
            BucketClient,
            ILogger,
            ILoadingIndicator,
            CancellationToken>(
            ListBadgeHandler.ListAsync);

        var deleteBadgeHandlerCommand = new Command(
            "delete",
            "Delete a badge.")
        {
            CommonInput.EnvironmentNameOption,
            CommonInput.CloudProjectIdOption,
            CloudContentDeliveryInput.BucketNameOption,
            CloudContentDeliveryInput.BadgeNameArgument
        };

        deleteBadgeHandlerCommand.SetHandler<
            CloudContentDeliveryInput,
            IUnityEnvironment,
            BadgeClient,
            BucketClient,
            ILogger,
            ILoadingIndicator,
            CancellationToken>(
            DeleteBadgeHandler.DeleteAsync);

        var badgeHandlerCommand = new Command(
            "badges",
            "Manage badges for a release.")
        {
            createBadgeHandlerCommand,
            listBadgeHandlerCommand,
            deleteBadgeHandlerCommand
        };
        return badgeHandlerCommand;
    }
}
