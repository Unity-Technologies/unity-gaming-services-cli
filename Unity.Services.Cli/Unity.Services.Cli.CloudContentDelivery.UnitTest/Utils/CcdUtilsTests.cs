using System.Net;
using System.Runtime.InteropServices;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using Unity.Services.Cli.CloudContentDelivery.Utils;
using Unity.Services.Cli.Common.Exceptions;
using Unity.Services.Gateway.ContentDeliveryManagementApiV1.Generated.Client;
using Unity.Services.Gateway.ContentDeliveryManagementApiV1.Generated.Model;

namespace CloudContentDeliveryTest.Utils;

[TestFixture]
public class CcdUtilsTests
{
    ApiResponse<List<CcdGetBucket200Response>> m_ValidApiResponse = null!;
    ApiResponse<List<CcdGetBucket200Response>> m_MissingContentRangeApiResponse = null!;

    [SetUp]
    public void SetUp()
    {
        var validHeaders = new Multimap<string, string>
        {
            { "Content-Range", "1-10/100" }
        };

        m_ValidApiResponse = new ApiResponse<List<CcdGetBucket200Response>>(
            HttpStatusCode.OK,
            validHeaders,
            new List<CcdGetBucket200Response>()
        );

        m_MissingContentRangeApiResponse = new ApiResponse<List<CcdGetBucket200Response>>(
            HttpStatusCode.OK,
            new Multimap<string, string>(),
            new List<CcdGetBucket200Response>()
        );
    }

    [Test]
    public void GetPaginationInformation_ShouldReturnCorrectString()
    {
        var result = CcdUtils.GetPaginationInformation(m_ValidApiResponse);
        Assert.That(result, Is.EqualTo("Listing 1-10/100"));
    }

    [Test]
    public void GetPaginationInformation_WithApiResponseMissingContentRange_ShouldReturnNull()
    {
        var result = CcdUtils.GetPaginationInformation(m_MissingContentRangeApiResponse);
        Assert.That(result, Is.Null);
    }

    [Test]
    public void ValidateBucketIdIsPresent_ValidGuid_DoesNotThrowException()
    {
        Assert.DoesNotThrow(() => CcdUtils.ValidateBucketIdIsPresent("00000000-0000-0000-0000-000000000000"));
    }

    [Test]
    public void ValidateBucketIdIsPresent_InvalidGuid_ThrowsException()
    {
        Assert.Throws<CliException>(() => CcdUtils.ValidateBucketIdIsPresent("invalid-guid"));
    }

    [Test]
    public void ValidateBucketIdIsPresent_NullBucketId_ThrowsException()
    {
        Assert.Throws<CliException>(() => CcdUtils.ValidateBucketIdIsPresent(null));
    }

    [Test]
    public void ValidateBucketIdIsPresent_EmptyString_ThrowsException()
    {
        Assert.Throws<CliException>(() => CcdUtils.ValidateBucketIdIsPresent(string.Empty));
    }

    [Test]
    public void ValidateParseMetadata_Valid_ReturnJsonObject()
    {

        var validMetadata = new
        {
            test = new
            {
                subkey = "value",
                subkey2 = "value"
            }
        };

        var validMetadataJson = JsonConvert.SerializeObject(validMetadata);
        var expectedJObject = JObject.Parse(validMetadataJson);
        var result = CcdUtils.ParseMetadata(validMetadataJson);
        Assert.That(result, Is.EqualTo(expectedJObject), "The parsed metadata should match the expected JSON object.");

        result = CcdUtils.ParseMetadata("");
        Assert.That(result, Is.Null, "The result should be null when the input is an empty string.");
    }

    [Test]
    public void ValidateParseMetadata_InvalidJson_ThrowsException()
    {
        Assert.Throws<CliException>(() => CcdUtils.ParseMetadata("{'partial'"));
        Assert.Throws<CliException>(() => CcdUtils.ParseMetadata("[invalid]"));
    }

    [Test]
    public void AdjustPathForPlatform_ValidInput_ReturnsAdjustedPath()
    {
        const string input = "images/image.jpg";
        const string expectedBackwardSlashes = "images\\image.jpg";
        const string expectedForwardSlashes = "images/image.jpg";

        string result = CcdUtils.AdjustPathForPlatform(input);

        if (Path.DirectorySeparatorChar == '\\')
        {
            Assert.That(result, Is.EqualTo(expectedBackwardSlashes), "The adjusted path should use backward slashes.");
        }
        else
        {
            Assert.That(result, Is.EqualTo(expectedForwardSlashes), "The adjusted path should use forward slashes.");
        }

    }

    [Test]
    public void AdjustPathForWindowsUsers_EmptyOrNullInput_ReturnsSame()
    {
        Assert.That(CcdUtils.AdjustPathForPlatform(""), Is.EqualTo(""), "The result should be an empty string when the input is an empty string.");
    }

    [Test]
    public void ConvertPathToForwardSlashes_ValidInput_ReturnsConvertedPath()
    {
        const string input = "images\\image.jpg";
        const string expected = "images/image.jpg";

        var result = CcdUtils.ConvertPathToForwardSlashes(input);
        Assert.That(result, Is.EqualTo(expected), "The converted path should match the expected result.");
    }

    [Test]
    public void ConvertPathToForwardSlashes_EmptyOrNullInput_ReturnsSame()
    {
        Assert.That(CcdUtils.ConvertPathToForwardSlashes(""), Is.EqualTo(""), "The result should be an empty string when the input is an empty string.");
    }

}
