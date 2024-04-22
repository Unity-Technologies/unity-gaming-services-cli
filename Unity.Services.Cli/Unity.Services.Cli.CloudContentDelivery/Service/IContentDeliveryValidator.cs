namespace Unity.Services.Cli.CloudContentDelivery.Service;

public interface IContentDeliveryValidator
{
    void ValidateProjectIdAndEnvironmentId(string projectId, string environmentId);
    void ValidateBucketId(string bucketId);
    void ValidateEntryId(string entryId);
    void ValidatePath(string path);
}
