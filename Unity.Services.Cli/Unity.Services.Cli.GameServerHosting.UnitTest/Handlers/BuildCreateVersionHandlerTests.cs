using System.Net;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using Spectre.Console;
using Unity.Services.Cli.Common.Console;
using Unity.Services.Cli.Common.Exceptions;
using Unity.Services.Cli.Common.Logging;
using Unity.Services.Cli.GameServerHosting.Exceptions;
using Unity.Services.Cli.GameServerHosting.Handlers;
using Unity.Services.Cli.GameServerHosting.Input;
using Unity.Services.Cli.TestUtils;
using Unity.Services.Gateway.GameServerHostingApiV1.Generated.Model;

namespace Unity.Services.Cli.GameServerHosting.UnitTest.Handlers;

[TestFixture]
partial class BuildCreateVersionHandlerTests : HandlerCommon
{
    [SetUp]
    public new void SetUp()
    {
        base.SetUp();

        SetUpTempFiles();
    }

    [Test]
    public async Task BuildCreateVersionAsync_CallsLoadingIndicatorStartLoading()
    {
        var mockLoadingIndicator = new Mock<ILoadingIndicator>();

        await BuildCreateVersionHandler.BuildCreateVersionAsync(
            null!,
            MockUnityEnvironment.Object,
            null!,
            null!,
            MockHttpClient!.Object,
            mockLoadingIndicator.Object,
            CancellationToken.None
        );

        mockLoadingIndicator.Verify(
            ex => ex
                .StartLoadingAsync(It.IsAny<string>(), It.IsAny<Func<StatusContext?, Task>>()),
            Times.Once);
    }

    [Test]
    public async Task BuildCreateVersionAsync_CallsFetchIdentifierAsync()
    {
        BuildCreateVersionInput input = new()
        {
            CloudProjectId = ValidProjectId,
            TargetEnvironmentName = ValidEnvironmentName,
            BuildId = ValidBuildIdContainer.ToString(),
            ContainerTag = ValidContainerTag
        };

        await BuildCreateVersionHandler.BuildCreateVersionAsync(
            input,
            MockUnityEnvironment.Object,
            GameServerHostingService!,
            MockLogger!.Object,
            MockHttpClient!.Object,
            CancellationToken.None);

        MockUnityEnvironment.Verify(ex => ex.FetchIdentifierAsync(CancellationToken.None), Times.Once);
    }
}
