using Unity.Services.Cli.CloudContentDelivery.Service;
using Unity.Services.Gateway.ContentDeliveryManagementApiV1.Generated.Model;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Unity.Services.Cli.CloudContentDelivery.Model;

public class LongOperationSummary : IOperationSummary
{

    public List<string> EntriesToAdd { get; private set; }
    public List<string> EntriesToUpdate { get; private set; }
    public List<string> EntriesToDelete { get; private set; }
    public List<string> EntriesToSkip { get; private set; }
    public ReleaseResult? Release { get; private set; }
    public BadgeResult? Badge { get; private set; }
    public string Operations { get; private set; }

    public double SynchronizationTimeInSeconds { get; private set; }
    public double TotalUploadedDataSizeInMb { get; private set; }
    public double AverageUploadSpeedInMbps { get; private set; }
    public int TotalNumberOfFilesUploaded { get; private set; }
    public bool OperationCompletedSuccessfully { get; private set; }

    public LongOperationSummary(
        SyncResult syncResult,
        bool operationCompletedSuccessfully,
        double synchronizationTimeInSeconds,
        double totalUploadedDataSizeInMb,
        double averageUploadSpeedInMbps,
        int totalNumberOfFilesUploaded,
        CcdGetBucket200ResponseLastRelease? release,
        CcdGetBucket200ResponseLastReleaseBadgesInner? badge)
    {

        EntriesToAdd = syncResult.EntriesToAdd.Select(entry => entry.Path).ToList();
        EntriesToUpdate = syncResult.EntriesToUpdate.Select(entry => entry.Path).ToList();
        EntriesToDelete = syncResult.EntriesToDelete.Select(entry => entry.Path).ToList();
        EntriesToSkip = syncResult.EntriesToSkip.Select(entry => entry.Path).ToList();

        Operations = syncResult.GetSummary();
        OperationCompletedSuccessfully = operationCompletedSuccessfully;
        SynchronizationTimeInSeconds = Math.Round(synchronizationTimeInSeconds, 4);
        TotalUploadedDataSizeInMb = Math.Round(totalUploadedDataSizeInMb, 4);
        AverageUploadSpeedInMbps = Math.Round(averageUploadSpeedInMbps, 4);
        TotalNumberOfFilesUploaded = totalNumberOfFilesUploaded;

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
