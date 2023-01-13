using System.Collections.Generic;
using System.Threading;
using Moq;
using Unity.Services.Gateway.CloudCodeApiV1.Generated.Api;
using Unity.Services.Gateway.CloudCodeApiV1.Generated.Model;

namespace Unity.Services.Cli.CloudCode.UnitTest.Mock;

class CloudCodeApiV1AsyncMock
{
    public const string PublishScriptAsyncScriptName = "test";

    public Mock<ICloudCodeApiAsync> DefaultApiAsyncObject = new();

    public ListScriptsResponse ListResponse { get; } = new(
        new List<ListScriptsResponseResultsInner>(), new ListScriptsResponseLinks(""));

    public GetScriptResponse GetResponse { get; set; } = new(
        "", ScriptType.API, Language.JS, new GetScriptResponseActiveScript("", 1, _params: new()), new());

    public PublishScriptResponse PublishScriptAsyncResponse { get; } = new(1);

    public readonly PublishScriptRequest PublishScriptAsyncRequestPayload = new()
    {
        _Version = 0
    };

    public void SetUp()
    {
        DefaultApiAsyncObject = new Mock<ICloudCodeApiAsync>();
        DefaultApiAsyncObject.Setup(
                a => a.ListScriptsAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<int?>(),
                    It.IsAny<string?>(),
                    It.IsAny<int>(),
                    CancellationToken.None))
            .ReturnsAsync(ListResponse);

        // Mocking PublishScript Response
        DefaultApiAsyncObject.Setup(
                a => a.PublishScriptAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    PublishScriptAsyncScriptName,
                    PublishScriptAsyncRequestPayload,
                    It.IsAny<int>(),
                    CancellationToken.None))
            .ReturnsAsync(PublishScriptAsyncResponse);

        DefaultApiAsyncObject.Setup(
                a => a.GetScriptAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<int>(),
                    CancellationToken.None))
            .ReturnsAsync(GetResponse);

        DefaultApiAsyncObject.Setup(a => a.Configuration)
            .Returns(new Gateway.CloudCodeApiV1.Generated.Client.Configuration());

        DefaultApiAsyncObject.Setup(
            ex => ex.DeleteScriptWithHttpInfoAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()));
    }
}
