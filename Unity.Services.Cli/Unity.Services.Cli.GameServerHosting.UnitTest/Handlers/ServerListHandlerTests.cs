using Moq;
using Spectre.Console;
using Unity.Services.Cli.Common.Console;
using Unity.Services.Cli.GameServerHosting.Handlers;
using Unity.Services.Cli.GameServerHosting.Input;
using Unity.Services.Gateway.GameServerHostingApiV1.Generated.Model;

namespace Unity.Services.Cli.GameServerHosting.UnitTest.Handlers;

[TestFixture]
class ServerListHandlerTests : HandlerCommon
{
    [Test]
    public async Task ServerListAsync_CallsLoadingIndicatorStartLoading()
    {
        var mockLoadingIndicator = new Mock<ILoadingIndicator>();

        await ServerListHandler.ServerListAsync(
            null!,
            MockUnityEnvironment.Object,
            null!,
            null!,
            mockLoadingIndicator.Object,
            CancellationToken.None);

        mockLoadingIndicator.Verify(
            ex => ex.StartLoadingAsync(
                It.IsAny<string>(),
                It.IsAny<Func<StatusContext?, Task>>()
            ),
            Times.Once
        );
    }

    [Test]
    public async Task ServerListAsync_CallsFetchIdentifierAsync()
    {
        ServerListInput input = new()
        {
            CloudProjectId = ValidProjectId,
            TargetEnvironmentName = ValidEnvironmentName
        };

        await ServerListHandler.ServerListAsync(
            input,
            MockUnityEnvironment.Object,
            GameServerHostingService!,
            MockLogger!.Object,
            CancellationToken.None
        );

        MockUnityEnvironment.Verify(ex => ex.FetchIdentifierAsync(CancellationToken.None), Times.Once);
    }

    [TestCase(
        null,
        null,
        null,
        null)]
    [TestCase(
        InvalidFleetId,
        ValidBuildConfigurationId,
        ValidServerId,
        Server.StatusEnum.ONLINE)]
    [TestCase(
        ValidFleetId,
        InvalidBuildConfigurationId,
        ValidServerId,
        Server.StatusEnum.ONLINE)]
    [TestCase(
        ValidFleetId,
        ValidBuildConfigurationId,
        InvalidServerId,
        Server.StatusEnum.ONLINE)]
    [TestCase(
        ValidFleetId,
        ValidBuildConfigurationId,
        ValidServerId,
        Server.StatusEnum.READY)]
    public async Task ServerListAsync_CallsListService(
        string? fleetId,
        long? buildConfigurationId,
        long? partial,
        Server.StatusEnum? status
    )
    {
        Guid? fleetGuid = fleetId == null ? null : new Guid(fleetId);

        var buildConfigurationIdString = buildConfigurationId == null ? null : buildConfigurationId.ToString();

        var partialString = partial == null ? null : partial.ToString();

        var statusString = status == null ? null : status.ToString();

        ServerListInput input = new()
        {
            CloudProjectId = ValidProjectId,
            TargetEnvironmentName = ValidEnvironmentName,
            FleetId = fleetId,
            BuildConfigurationId = buildConfigurationIdString,
            Partial = partialString,
            Status = statusString
        };

        await ServerListHandler.ServerListAsync(
            input,
            MockUnityEnvironment.Object,
            GameServerHostingService!,
            MockLogger!.Object,
            CancellationToken.None
        );

        ServersApi!.DefaultServersClient.Verify(
            api => api.ListServersAsync(
                new Guid(input.CloudProjectId),
                new Guid(ValidEnvironmentId),
                null,
                null,
                null,
                null,
                null,
                fleetGuid,
                null,
                null,
                buildConfigurationIdString,
                null,
                partialString,
                statusString,
                0,
                CancellationToken.None
            ),
            Times.Once
        );
    }
}
