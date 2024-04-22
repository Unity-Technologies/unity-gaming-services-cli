using Unity.Services.Cli.Common.Exceptions;
using Unity.Services.Gateway.ContentDeliveryManagementApiV1.Generated.Model;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Unity.Services.Cli.CloudContentDelivery.Model;

public class BucketResult
{
    public BucketResult(
        CcdGetBucket200Response? response
    )
    {
        if (response == null)
            throw new CliException(
                "A server error occurred while retrieving the bucket result. Please try again later.",
                ExitCode.HandledError);

        Id = response.Id;
        Name = response.Name;
        Created = response.Created;
        Description = response.Description;
        EnvironmentName = response.EnvironmentName;
        EnvironmentId = response.EnvironmentId;
        Projectguid = response.Projectguid;
        Private = response.Private;
        Permissions = response.Permissions;
        LastRelease = response.LastRelease;
        Changes = response.Changes;
        Attributes = response.Attributes;
    }

    public Guid Id { get; set; }

    public string? Name { get; set; }

    public string? Description { get; set; }

    public bool Private { get; set; }

    public Guid EnvironmentId { get; set; }

    public string? EnvironmentName { get; set; }

    public Guid Projectguid { get; set; }

    public CcdGetBucket200ResponseAttributes? Attributes { get; set; }

    public CcdGetBucket200ResponseChanges? Changes { get; set; }

    public DateTime Created { get; set; }

    public CcdGetBucket200ResponseLastRelease? LastRelease { get; set; }

    public CcdGetBucket200ResponsePermissions? Permissions { get; set; }

    public override string ToString()
    {
        var serializer = new SerializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .DisableAliases()
            .Build();
        return serializer.Serialize(this);
    }
}
