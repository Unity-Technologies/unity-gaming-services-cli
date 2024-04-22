using Unity.Services.Cli.CloudContentDelivery.Service;
using Unity.Services.Gateway.ContentDeliveryManagementApiV1.Generated.Model;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Unity.Services.Cli.CloudContentDelivery.Model;

public class ShortOperationSummary : IOperationSummary
{

    public ReleaseResult? Release { get; private set; }
    public BadgeResult? Badge { get; private set; }
    public string Operations { get; private set; }
    public double SynchronizationTimeInSeconds { get; private set; }

    public bool OperationCompletedSuccessfully { get; private set; }

    public ShortOperationSummary(
        SyncResult syncResult,
        bool operationCompletedSuccessfully,
        double synchronizationTimeInSeconds,
        CcdGetBucket200ResponseLastRelease? release,
        CcdGetBucket200ResponseLastReleaseBadgesInner? badge)
    {

        Operations = syncResult.GetSummary();
        OperationCompletedSuccessfully = operationCompletedSuccessfully;
        SynchronizationTimeInSeconds = Math.Round(synchronizationTimeInSeconds, 4);

        if (release != null)
            Release = new ReleaseResult(release);
        if (badge != null)
            Badge = new BadgeResult(badge);
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
