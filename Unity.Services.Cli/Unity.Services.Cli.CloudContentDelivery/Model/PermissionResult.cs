using Unity.Services.Cli.Common.Exceptions;
using Unity.Services.Gateway.ContentDeliveryManagementApiV1.Generated.Model;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Unity.Services.Cli.CloudContentDelivery.Model;

public class PermissionResult
{
    public PermissionResult(
        CcdGetAllByBucket200ResponseInner? response
    )
    {
        if (response == null)
            throw new CliException(
                "A server error occurred while retrieving the permission result. Please try again later.",
                ExitCode.HandledError);

        Action = response.Action;
        Permission = response.Permission;
        Role = response.Role;
    }

    public string Action { get; set; }
    public string Permission { get; set; }
    public string Role { get; set; }

    public override string ToString()
    {
        var serializer = new SerializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .DisableAliases()
            .Build();
        return serializer.Serialize(this);
    }
}
