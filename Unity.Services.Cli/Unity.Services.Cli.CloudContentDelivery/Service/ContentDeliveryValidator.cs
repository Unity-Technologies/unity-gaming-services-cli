using Unity.Services.Cli.Common.Models;
using Unity.Services.Cli.Common.Validator;

namespace Unity.Services.Cli.CloudContentDelivery.Service;

public class ContentDeliveryValidator : IContentDeliveryValidator
{
    readonly IConfigurationValidator m_ConfigValidator;

    public ContentDeliveryValidator(IConfigurationValidator configValidator)
    {
        m_ConfigValidator = configValidator ?? throw new ArgumentNullException(nameof(configValidator));
    }

    public void ValidateProjectIdAndEnvironmentId(string projectId, string environmentId)
    {
        ValidateConfig(Keys.ConfigKeys.ProjectId, projectId);
        ValidateConfig(Keys.ConfigKeys.EnvironmentId, environmentId);
    }

    public void ValidateBucketId(string bucketId)
    {
        ValidateConfig(Keys.ConfigKeys.BucketName, bucketId);
    }

    public void ValidateEntryId(string entryId)
    {
        if (!Guid.TryParse(entryId, out _)) throw new ArgumentException("Invalid entryId. It must be a valid GUID.");
    }

    public void ValidatePath(string? path)
    {
        if (string.IsNullOrEmpty(path)) throw new ArgumentException("Invalid Path. The path must not be empty.");
    }

    void ValidateConfig(string key, string value)
    {
        m_ConfigValidator.ThrowExceptionIfConfigInvalid(key, value);
    }
}
