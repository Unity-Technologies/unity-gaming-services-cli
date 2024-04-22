using Unity.Services.Cli.CloudContentDelivery.Model;

namespace Unity.Services.Cli.CloudContentDelivery.Service;

public interface IOperationSummary
{

    ReleaseResult? Release { get; }
    BadgeResult? Badge { get; }

    public string Operations { get; }
    public double SynchronizationTimeInSeconds { get; }

    public bool OperationCompletedSuccessfully { get; }
}
