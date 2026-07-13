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
        ModuleRootCommand = new Command("ccd", new CommandDescription("Manage Cloud Content Delivery.")
            .WithDocs("https://docs.unity.com/ugs/manual/ccd/manual")
            .WithAdminApi("https://services.docs.unity.com/content-delivery-management/v1/")
            .Build());
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
            new CommandDescription("List buckets for a project.")
                .WithReturn("List of buckets with id and name.")
                .Build())
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
            new CommandDescription("Create bucket for a project.")
                .WithReturn("Bucket details with id, name, description, private, environmentId, environmentName, and permissions.")
                .Build())
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
            new CommandDescription("Delete buckets.")
                .WithReturn("Confirmation message.")
                .Build())
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
            new CommandDescription("Get bucket info.")
                .WithReturn("Bucket details with id, name, description, private, environmentId, environmentName, and permissions.")
                .Build())
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
            new CommandDescription("Update permissions for a bucket.")
                .WithReturn("The updated permission with action, permission, and role.")
                .Build())
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
            new CommandDescription("Create release from latest version of current bucket.")
                .WithReturn("Release details with releaseId, releaseNum, contentSize, contentHash, badges, notes, and metadata.")
                .Build())
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
            new CommandDescription("Get release info for specific release.")
                .WithReturn("Release details with releaseId, releaseNum, contentSize, contentHash, badges, notes, and metadata.")
                .Build())
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
            new CommandDescription("List releases for current bucket.")
                .WithReturn("List of releases with releaseId and releaseNum.")
                .Build())
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
            new CommandDescription("Promote release to another bucket.")
                .WithReturn("The promotionId for tracking the promotion status.")
                .Build())
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
            new CommandDescription("Check promotion status.")
                .WithReturn("Promotion details with promotionId, promotionStatus, source/target bucket and environment info.")
                .Build())
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
            new CommandDescription("Update an existing Release.")
                .WithReturn("Release details with releaseId, releaseNum, contentSize, contentHash, badges, notes, and metadata.")
                .Build())
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
            new CommandDescription("Create entry for current bucket from a local file.")
                .WithReturn("Entry details with entryid, path, currentVersionid, contentType, contentSize, contentHash, labels, and metadata.")
                .Build())
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
            new CommandDescription("Delete entry from current bucket.")
                .WithReturn("Confirmation message.")
                .Build())
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
            new CommandDescription("Download entry content from current bucket.")
                .WithReturn("Writes file content to a local file named after the entry.")
                .Build())
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
            new CommandDescription("Get entry info from current bucket.")
                .WithReturn("Entry details with entryid, path, currentVersionid, contentType, contentSize, contentHash, labels, and metadata.")
                .Build())
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
            new CommandDescription("List entries for current bucket.")
                .WithReturn("List of entries with id and name (path).")
                .Build())
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
            new CommandDescription(
                "Sync entries from local directory for current bucket.\n"
                + "Automatically creates, updates, and deletes entries\n"
                + "within the bucket to match the files in the local directory.")
                .WithReturn("Operation summary with counts of added, updated, deleted, and skipped entries; includes release and badge info if requested.")
                .Build())
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
            CloudContentDeliveryInput.ConcurrentUploadRequestsOption,
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
            new CommandDescription("Update entry for current bucket.")
                .WithReturn("Entry details with entryid, path, currentVersionid, contentType, contentSize, contentHash, labels, and metadata.")
                .Build())
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
            new CommandDescription("Create a new badge or move an existing one.")
                .WithReturn("Badge details with name, releaseId, releaseNum, and created timestamp.")
                .Build())
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
            new CommandDescription("List badges in the current bucket.")
                .WithReturn("List of badges with name, releaseId, and releaseNum.")
                .Build())
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
            new CommandDescription("Delete a badge.")
                .WithReturn("Confirmation message.")
                .Build())
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
