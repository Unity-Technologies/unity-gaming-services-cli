using Unity.Services.Cli.Common.Exceptions;
using Unity.Services.Gateway.ContentDeliveryManagementApiV1.Generated.Model;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Unity.Services.Cli.CloudContentDelivery.Model;

public class PromotionResult : CcdGetPromotions200ResponseInner
{
    public PromotionResult(
        CcdGetPromotions200ResponseInner? response
    )
    {
        if (response == null)
            throw new CliException(
                "A server error occurred while retrieving the release result. Please try again later.",
                ExitCode.HandledError);

        PromotionId = response.PromotionId;
        PromotionStatus = response.PromotionStatus;
        FromBucketName = response.FromBucketName;
        FromBucketId = response.FromBucketId;
        FromEnvironmentId = response.FromEnvironmentId;
        FromEnvironmentName = response.FromEnvironmentName;
        FromReleaseId = response.FromReleaseId;
        FromReleaseNumber = response.FromReleaseNumber;
        Error = response.Error;
        ToBucketId = response.ToBucketId;
        ToEnvironmentId = response.ToEnvironmentId;
        ToReleaseId = response.ToReleaseId;
        ToBucketName = response.ToBucketName;
        ToEnvironmentName = response.ToEnvironmentName;

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
