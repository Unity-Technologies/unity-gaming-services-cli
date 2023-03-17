using System;
using System.Collections.Generic;
using System.IO;
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

    public ListModulesResponse ListModulesResponse { get; } = new(new List<ListModulesResponseResultsInner>(), "");
    public GetScriptResponse GetResponse { get; set; } = new(
        "", ScriptType.API, Language.JS, new GetScriptResponseActiveScript("", 1, _params: new()), new());

    public GetModuleResponse GetModuleResponse { get; set; } =
        new("", Language.CS);
    public PublishScriptResponse PublishScriptAsyncResponse { get; } = new(1);

    public readonly PublishScriptRequest PublishScriptAsyncRequestPayload = new()
    {
        _Version = 0
    };

    public CreateModuleResponse CreateModuleResponse { get; set; } = new(DateTime.Now);
    public UpdateModuleResponse UpdateModuleResponse { get; set; } = new(DateTime.Now);

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

        DefaultApiAsyncObject.Setup(
                ex => ex.GetModuleAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<int>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(GetModuleResponse);

        DefaultApiAsyncObject.Setup(
                ex => ex.ListModulesAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<int?>(),
                    It.IsAny<string?>(),
                    It.IsAny<int>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(ListModulesResponse);

        DefaultApiAsyncObject.Setup(
                ex => ex.DeleteModuleWithHttpInfoAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<int>(),
                    It.IsAny<CancellationToken>()));

        DefaultApiAsyncObject.Setup(
            ex => ex.CreateModuleAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<Language>(),
                It.IsAny<Stream>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateModuleResponse);

        DefaultApiAsyncObject.Setup(
            ex => ex.UpdateModuleAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<Stream>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(UpdateModuleResponse);;
    }

}
