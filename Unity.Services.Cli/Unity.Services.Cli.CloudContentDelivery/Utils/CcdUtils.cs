using Newtonsoft.Json;
using Unity.Services.Cli.Common.Exceptions;
using Unity.Services.Gateway.ContentDeliveryManagementApiV1.Generated.Client;

namespace Unity.Services.Cli.CloudContentDelivery.Utils;

public static class CcdUtils
{

    public static string? GetPaginationInformation<T>(ApiResponse<List<T>> result)
    {
        if (!result.Headers.TryGetValue("Content-Range", out var contentRangeValues)) return null;
        var contentRange = contentRangeValues.FirstOrDefault();
        return $"Listing {contentRange}";
    }

    public static object? ParseMetadata(string metadata)
    {
        try
        {
            var result = JsonConvert.DeserializeObject<object>(metadata);
            return result;
        }
        catch (JsonException ex)
        {
            throw new CliException(
                $"The metadata could not be parsed successfully: {ex.Message}",
                ExitCode.HandledError);
        }
    }

    public static void ValidateBucketIdIsPresent(string? bucketId)
    {
        if (bucketId == null || !Guid.TryParse(bucketId, out _))
        {
            throw new CliException(
                @"A valid bucket-name is required to execute this command.

You can specify the bucket-name using one of the following methods:
     1. Set the bucket-name in your configuration once for all future commands:
            ugs config set bucket-name <bucket-name>
     2. Include the -b <bucket-name> option when running the command.

     3. Set the UGS_CLI_BUCKET_NAME environment variable to the desired bucket-name for all future commands.",
                ExitCode.HandledError);

        }
    }

}
