using System.IO.Abstractions.TestingHelpers;
using System.Net;
using Moq;
using Moq.Protected;
using NUnit.Framework;
using Unity.Services.Cli.CloudContentDelivery.Service;

namespace CloudContentDeliveryTest.Service;

[TestFixture]
public class UploadContentClientTests
{
    UploadContentClient m_UploadContentClient = null!;
    readonly Mock<HttpMessageHandler> m_MessageHandlerMock = new(MockBehavior.Strict);
    MockFileSystem m_FileSystemMock = null!;

    [SetUp]
    public void SetUp()
    {

        var file1 = new MockFileData("Content of file 1");
        var file2 = new MockFileData("Content of file 2");

        var fileSystemEntries = new Dictionary<string, MockFileData>
        {
            { @"local-folder/file1.txt", file1 },
            { @"local-folder/file2.txt", file2 }
        };

        m_FileSystemMock = new MockFileSystem(fileSystemEntries);
        var httpClient = new HttpClient(m_MessageHandlerMock.Object);
        m_UploadContentClient = new UploadContentClient(httpClient);
    }

    [Test]
    public async Task UploadContentToCcd_Success_ReturnsSuccessfulResponse()
    {
        var httpClientMock = new Mock<HttpMessageHandler>();
        var mockResponse = new HttpResponseMessage(HttpStatusCode.OK);
        httpClientMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(mockResponse);

        var httpClient = new HttpClient(httpClientMock.Object);
        var uploadClient = new UploadContentClient(httpClient);
        await using var fileStream = m_FileSystemMock.File.OpenRead(@"local-folder/file1.txt");
        var response = await uploadClient.UploadContentToCcd("https://www.unity.com/fakesignedurl/upload", fileStream);
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }

    [Test]
    public async Task UploadContentToCcd_Failure_ReturnsErrorResponse()
    {
        var httpClientMock = new Mock<HttpMessageHandler>();
        var mockResponse = new HttpResponseMessage(HttpStatusCode.BadRequest);
        httpClientMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(mockResponse);

        await using var fileStream = m_FileSystemMock.File.OpenRead(@"local-folder/file1.txt");
        var httpClient = new HttpClient(httpClientMock.Object);
        var uploadClient = new UploadContentClient(httpClient);
        var response = await uploadClient.UploadContentToCcd("https://www.unity.com/fakesignedurl/upload", fileStream);
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test]
    public void GetContentType_ShouldReturnContentType()
    {
        var localPath = "example.txt";
        var localPath2 = "example.jpg";
        var localPath3 = "example.bin";

        var result = m_UploadContentClient.GetContentType(localPath);
        var result2 = m_UploadContentClient.GetContentType(localPath2);
        var result3 = m_UploadContentClient.GetContentType(localPath3);

        Assert.That(result, Is.EqualTo("text/plain"));
        Assert.That(result2, Is.EqualTo("image/jpeg"));
        Assert.That(result3, Is.EqualTo("application/octet-stream"));
    }

}
