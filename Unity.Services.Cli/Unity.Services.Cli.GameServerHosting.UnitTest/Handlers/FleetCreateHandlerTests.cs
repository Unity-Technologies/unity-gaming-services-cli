using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json;
using Spectre.Console;
using Unity.Services.Cli.Common.Console;
using Unity.Services.Cli.Common.Logging;
using Unity.Services.Cli.GameServerHosting.Exceptions;
using Unity.Services.Cli.GameServerHosting.Handlers;
using Unity.Services.Cli.GameServerHosting.Input;
using Unity.Services.Cli.TestUtils;
using Unity.Services.Gateway.GameServerHostingApiV1.Generated.Model;
using Region = Unity.Services.Gateway.GameServerHostingApiV1.Generated.Model.Region;

namespace Unity.Services.Cli.GameServerHosting.UnitTest.Handlers;

[TestFixture]
class FleetCreateHandlerTests : HandlerCommon
{
    [Test]
    public async Task FleetCreateAsync_CallsLoadingIndicatorStartLoading()
    {
        var mockLoadingIndicator = new Mock<ILoadingIndicator>();

        await FleetCreateHandler.FleetCreateAsync(null!, MockUnityEnvironment.Object, null!, null!,
            mockLoadingIndicator.Object, CancellationToken.None);

        mockLoadingIndicator.Verify(ex => ex
            .StartLoadingAsync(It.IsAny<string>(), It.IsAny<Func<StatusContext?, Task>>()), Times.Once);
    }

    [Test]
    public async Task FleetCreateAsync_CallsFetchIdentifierAsync()
    {
        FleetCreateInput input = new()
        {
            CloudProjectId = ValidProjectId,
            TargetEnvironmentName = ValidEnvironmentName,
            FleetName = ValidFleetName,
            OsFamily = FleetCreateRequest.OsFamilyEnum.LINUX,
            BuildConfigurations = new[]
            {
                ValidBuildConfigurationId
            },
            Regions = new[]
            {
                ValidRegionId
            },
            UsageSettings = new[]
            {
                ValidUsageSettingsJson
            }
        };


        await FleetCreateHandler.FleetCreateAsync(
            input,
            MockUnityEnvironment.Object,
            GameServerHostingService!,
            MockLogger!.Object,
            CancellationToken.None
        );

        MockUnityEnvironment.Verify(ex => ex.FetchIdentifierAsync(CancellationToken.None), Times.Once);
    }

    [TestCase(ValidProjectId, ValidEnvironmentName, ValidFleetName,
        FleetCreateRequest.OsFamilyEnum.LINUX,
        new[]
        {
            ValidBuildConfigurationId
        },
        new[]
        {
            ValidRegionId
        },
        new[]{
            ValidUsageSettingsJson
        }
    )]
    public async Task FleetCreateAsync_CallsCreateService(string projectId, string environmentName, string fleetName,
        FleetCreateRequest.OsFamilyEnum osFamily, long[] buildConfigurations, string[] regions, string[] usageSettings)
    {
        FleetCreateInput input = new()
        {
            CloudProjectId = projectId,
            TargetEnvironmentName = environmentName,
            FleetName = fleetName,
            OsFamily = osFamily,
            BuildConfigurations = buildConfigurations,
            Regions = regions,
            UsageSettings = usageSettings
        };

        await FleetCreateHandler.FleetCreateAsync(input, MockUnityEnvironment.Object, GameServerHostingService!,
            MockLogger!.Object, CancellationToken.None);

        var regionList = regions.Select(r => new Region(regionID: new Guid(r))).ToList();

        var usageSetting = JsonConvert.DeserializeObject<FleetUsageSetting>(ValidUsageSettingsJson);

        var createRequest = new FleetCreateRequest(name: input.FleetName, osFamily: input.OsFamily,
            buildConfigurations: buildConfigurations.ToList(), regions: regionList, usageSettings: new List<FleetUsageSetting> { usageSetting! });

        FleetsApi!.DefaultFleetsClient.Verify(api => api.CreateFleetAsync(
            new Guid(input.CloudProjectId), new Guid(ValidEnvironmentId), null,
            createRequest, 0, CancellationToken.None
        ), Times.Once);
    }

    [TestCase(ValidProjectId, ValidEnvironmentName, null, FleetCreateRequest.OsFamilyEnum.LINUX,
        new[]
        {
            ValidBuildConfigurationId
        },
        new[]
        {
            ValidRegionId
        },
        TestName = "Missing Fleet Name"
    )]
    [TestCase(ValidProjectId, ValidEnvironmentName, ValidFleetName, null,
        new[]
        {
            ValidBuildConfigurationId
        },
        new[]
        {
            ValidRegionId
        },
        TestName = "Missing OS Family"
    )]
    [TestCase(ValidProjectId, ValidEnvironmentName, ValidFleetName, FleetCreateRequest.OsFamilyEnum.LINUX,
        null,
        new[]
        {
            ValidRegionId
        },
        TestName = "Missing build configurations"
    )]
    [TestCase(ValidProjectId, ValidEnvironmentName, ValidFleetName, FleetCreateRequest.OsFamilyEnum.LINUX,
        new[]
        {
            ValidBuildConfigurationId
        },
        null,
        TestName = "Missing regions"
    )]
    [TestCase(ValidProjectId, ValidEnvironmentName, ValidFleetName, FleetCreateRequest.OsFamilyEnum.LINUX,
        new long[0],
        new[]
        {
            ValidRegionId
        },
        TestName = "Empty build configurations"
    )]
    [TestCase(ValidProjectId, ValidEnvironmentName, ValidFleetName, FleetCreateRequest.OsFamilyEnum.LINUX,
        new[]
        {
            ValidBuildConfigurationId
        },
        new string[0],
        TestName = "Empty build regions"
    )]
    public Task FleetCreateAsync_MissingInputThrowsException(string? projectId, string? environmentName,
        string? fleetName,
        FleetCreateRequest.OsFamilyEnum? osFamily, long[] buildConfigurations, string[] regions)
    {
        FleetCreateInput input = new()
        {
            CloudProjectId = projectId,
            TargetEnvironmentName = environmentName,
            FleetName = fleetName,
            OsFamily = osFamily,
            BuildConfigurations = buildConfigurations,
            Regions = regions
        };

        Assert.ThrowsAsync<MissingInputException>(() =>
            FleetCreateHandler.FleetCreateAsync(input,
                MockUnityEnvironment.Object,
                GameServerHostingService!,
                MockLogger!.Object,
                CancellationToken.None
            )
        );

        TestsHelper.VerifyLoggerWasCalled(MockLogger!, LogLevel.Critical, LoggerExtension.ResultEventId, Times.Never);
        return Task.CompletedTask;
    }

    [TestCase(ValidProjectId, ValidEnvironmentName, ValidFleetName,
        FleetCreateRequest.OsFamilyEnum.LINUX,
        new[]
        {
            ValidBuildConfigurationId
        },
        new[]
        {
            ValidRegionId
        },
        new[]{
            "badjson"
        }
    )]
    public Task FleetCreateAsync_InvalidUsageSettingsJsonThrowsException(string? projectId, string? environmentName,
        string? fleetName,
        FleetCreateRequest.OsFamilyEnum? osFamily, long[] buildConfigurations, string[] regions, string[] usageSettings)
    {
        FleetCreateInput input = new()
        {
            CloudProjectId = projectId,
            TargetEnvironmentName = environmentName,
            FleetName = fleetName,
            OsFamily = osFamily,
            BuildConfigurations = buildConfigurations,
            Regions = regions,
            UsageSettings = usageSettings
        };

        Assert.ThrowsAsync<JsonReaderException>(() =>
            FleetCreateHandler.FleetCreateAsync(input,
                MockUnityEnvironment.Object,
                GameServerHostingService!,
                MockLogger!.Object,
                CancellationToken.None
            )
        );

        TestsHelper.VerifyLoggerWasCalled(MockLogger!, LogLevel.Critical, LoggerExtension.ResultEventId, Times.Never);
        return Task.CompletedTask;
    }
}
