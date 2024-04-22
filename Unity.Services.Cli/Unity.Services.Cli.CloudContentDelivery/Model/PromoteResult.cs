using Unity.Services.Cli.Common.Exceptions;
using Unity.Services.Gateway.ContentDeliveryManagementApiV1.Generated.Model;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Unity.Services.Cli.CloudContentDelivery.Model;

public class PromoteResult : CcdPromoteBucketAsync200Response
{
    public PromoteResult(
        CcdPromoteBucketAsync200Response? response
    )
    {
        if (response == null)
            throw new CliException(
                "A server error occurred while retrieving the promote result. Please try again later.",
                ExitCode.HandledError);

        PromotionId = response.PromotionId;
    }

    public override string ToString()
    {
        var serializer = new SerializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .DisableAliases()
            .Build();
        return serializer.Serialize(this);
    }
}
