using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Moq.Protected;
using NUnit.Framework;
using Unity.Services.Cli.CloudCode.Deploy;
using Unity.Services.CloudCode.Authoring.Editor.Core.Model;
using Unity.Services.Gateway.CloudCodeApiV1.Generated.Model;
using Module = Unity.Services.Cli.CloudCode.Deploy.CloudCodeModule;
using Language = Unity.Services.CloudCode.Authoring.Editor.Core.Model.Language;

namespace Unity.Services.Cli.CloudCode.UnitTest.Deploy;

public class CloudCodeModuleDownloaderTests
{

    readonly Mock<HttpMessageHandler> m_MockHttpClientHandler = new();

    [SetUp]
    public void SetUp()
    {
        m_MockHttpClientHandler.Reset();
        m_MockHttpClientHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.OK });
    }

    [Test]
    [Ignore("This is not a unit test, either mock or turn into an integration test")]
    public async Task DownloadModule_Success()
    {
        var testModule = new Unity.Services.Cli.CloudCode.Deploy.CloudCodeModule(
            new ScriptName("test_a.zip"),
            Language.JS,
            Path.Join("project-id","env-id", "test_a" + ".ccm"));

        var client = new HttpClient(m_MockHttpClientHandler.Object)
        {
            BaseAddress = new Uri("https://localhost:8080")
        };

        var cloudCodeModulesDownloader = new CloudCodeModulesDownloader(client);
        var actual = await cloudCodeModulesDownloader.DownloadModule(
            testModule,
            new CancellationToken());

        Assert.IsInstanceOf(typeof(Stream), actual);
        m_MockHttpClientHandler.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req =>
                req.Method == HttpMethod.Get
            ),
            ItExpr.IsAny<CancellationToken>()
        );
    }
}
