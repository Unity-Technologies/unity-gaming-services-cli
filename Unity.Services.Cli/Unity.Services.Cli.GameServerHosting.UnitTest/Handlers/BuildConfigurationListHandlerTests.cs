using Moq;
using Spectre.Console;
using Unity.Services.Cli.Common.Console;
using Unity.Services.Cli.GameServerHosting.Handlers;
using Unity.Services.Cli.GameServerHosting.Input;

namespace Unity.Services.Cli.GameServerHosting.UnitTest.Handlers;

[TestFixture]
class BuildConfigurationListHandlerTests : HandlerCommon
{
    [Test]
    public async Task BuildConfigurationListAsync_CallsLoadingIndicatorStartLoading()
    {
        var mockLoadingIndicator = new Mock<ILoadingIndicator>();

        await BuildConfigurationListHandler.BuildConfigurationListAsync(null!, MockUnityEnvironment.Object, null!, null!,
            mockLoadingIndicator.Object, CancellationToken.None);

        mockLoadingIndicator.Verify(ex => ex
            .StartLoadingAsync(It.IsAny<string>(), It.IsAny<Func<StatusContext?, Task>>()), Times.Once);
    }

    [Test]
    public async Task BuildConfigurationListAsync_CallsFetchIdentifierAsync()
    {
        BuildConfigurationListInput input = new()
        {
            CloudProjectId = ValidProjectId,
            TargetEnvironmentName = ValidEnvironmentName
        };

        await BuildConfigurationListHandler.BuildConfigurationListAsync(
            input,
            MockUnityEnvironment.Object,
            GameServerHostingService!,
            MockLogger!.Object,
            CancellationToken.None
        );

        MockUnityEnvironment.Verify(ex => ex.FetchIdentifierAsync(CancellationToken.None), Times.Once);
    }

    [TestCase(null, null)]
    [TestCase(ValidFleetId, null)]
    [TestCase(null, ValidFleetName)]
    public async Task BuildConfigurationListAsync_CallsListService(
        string? fleetId,
        string? partial
    )
    {
        Guid? fleetGuid = fleetId == null ? null : new Guid(fleetId);

        BuildConfigurationListInput input = new()
        {
            CloudProjectId = ValidProjectId,
            TargetEnvironmentName = ValidEnvironmentName,
            FleetId = fleetId,
            Partial = partial
        };

        await BuildConfigurationListHandler.BuildConfigurationListAsync(
            input,
            MockUnityEnvironment.Object,
            GameServerHostingService!,
            MockLogger!.Object,
            CancellationToken.None
        );

        BuildConfigurationsApi!.DefaultBuildConfigurationsClient.Verify(api => api.ListBuildConfigurationsAsync(
            new Guid(input.CloudProjectId), new Guid(ValidEnvironmentId),
            fleetGuid, partial, 0, CancellationToken.None
        ), Times.Once);
    }
}
