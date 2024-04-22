using System.CommandLine;
using Unity.Services.Cli.Common.Input;
using Unity.Services.Gateway.ContentDeliveryManagementApiV1.Generated.Model;

namespace Unity.Services.Cli.CloudContentDelivery.Input;

public class CloudContentDeliveryInputBuckets : CloudContentDeliveryInput
{

    public static readonly Option<CcdUpdatePermissionByBucketRequest.RoleEnum> RoleOption = new(
        new[]
        {
            "-r",
            "--role"
        },
        "Sets the role (Client, User) affected by the permission change. User role can be changed for additional management permission (Write) and Client role can be changed to allow access to additional Client API endpoints (ListEntries, ListRelease).")
    {
        IsRequired = true
    };

    public static readonly Option<CcdUpdatePermissionByBucketRequest.ActionEnum> ActionOption = new(
        new[]
        {
            "-a",
            "--action"
        },
        "Determines the action (ListEntries, ListReleases, Write) for which the bucket permission will be updated.")
    {
        IsRequired = true
    };

    public static readonly Option<CcdUpdatePermissionByBucketRequest.PermissionEnum> PermissionOption = new(
        new[]
        {
            "-m",
            "--permission"
        },
        "Specifies whether to (Allow, Deny) the defined action.")
    {
        IsRequired = true
    };

    public static readonly Option<string> BucketDescriptionOption = new(
        new[]
        {
            "-d",
            "--description"
        },
        "Description of the bucket.");

    public static readonly Option<bool> BucketPrivateOption = new(
        new[]
        {
            "-i",
            "--private"
        },
        "Privacy setting of the bucket. Note that this setting cannot be changed once the bucket is created (default false).");

    public static readonly Option<string> SortByBucketOption = new(
        new[]
        {
            "-s",
            "--sort-by"
        },
        "Sort by values (name, created).");

    [InputBinding(nameof(PermissionOption))]
    public CcdUpdatePermissionByBucketRequest.PermissionEnum Permission { get; set; }


    [InputBinding(nameof(ActionOption))]
    public CcdUpdatePermissionByBucketRequest.ActionEnum Action { get; set; }


    [InputBinding(nameof(RoleOption))]
    public CcdUpdatePermissionByBucketRequest.RoleEnum Role { get; set; }

    [InputBinding(nameof(BucketDescriptionOption))]
    public string? BucketDescription { get; set; }

    [InputBinding(nameof(BucketPrivateOption))]
    public bool? BucketPrivate { get; set; }

    [InputBinding(nameof(SortByBucketOption))]
    public string? SortByBucket { get; set; }

}
