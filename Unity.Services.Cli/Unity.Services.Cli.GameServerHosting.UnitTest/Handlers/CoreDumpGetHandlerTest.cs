using Microsoft.Extensions.Logging;
using Moq;
using Unity.Services.Cli.Common.Exceptions;
using Unity.Services.Cli.Common.Logging;
using Unity.Services.Cli.GameServerHosting.Handlers;
using Unity.Services.Cli.GameServerHosting.Input;
using Unity.Services.Cli.TestUtils;

namespace Unity.Services.Cli.GameServerHosting.UnitTest.Handlers;

[TestFixture]
[TestOf(typeof(CoreDumpGetHandler))]
class CoreDumpGetHandlerTest : HandlerCommon
{
    [TestCase(
        ValidProjectId,
        ValidEnvironmentName,
        CoreDumpMockFleetIdWithEnabledConfig,
        "state: enabled",
        TestName = "Core Dump Config should be enabled")]
    [TestCase(
        ValidProjectId,
        ValidEnvironmentName,
        CoreDumpMockFleetIdWithDisabledConfig,
        "state: disabled",
        TestName = "Core Dump Config Should be disabled")]
    [TestCase(
        ValidProjectId,
        ValidEnvironmentName,
        CoreDumpMockFleetIdWithoutConfig,
        "",
        true,
        "Core Dump Storage is not configured",
        TestName = "Core Dump Config is not configured")]
    [TestCase(
        InvalidProjectId,
        ValidEnvironmentName,
        CoreDumpMockFleetIdWithoutConfig,
        "",
        true,
        "validation error",
        TestName = "Invalid Project Id, unexpected exception")]
    [TestCase(
        ValidProjectId,
        ValidEnvironmentName,
        CoreDumpMockFleetIdWithEnabledConfig,
        "state: enabled",
        true,
        "Missing value for input: 'fleet-id'",
        TestName = "Missing input")]
    public async Task CoreDumpGetAsync(
        string projectId,
        string environmentName,
        string fleetId,
        string outputContains,
        bool expectedException = false,
        string exceptionContains = "")
    {
        FleetIdInput input = new()
        {
            CloudProjectId = projectId,
            TargetEnvironmentName = environmentName,
            FleetId = fleetId
        };

        try
        {
            await CoreDumpGetHandler.CoreDumpGetAsync(
                input,
                MockUnityEnvironment.Object,
                GameServerHostingService!,
                MockLogger!.Object,
                CancellationToken.None
            );
        }
        catch (CliException e) when (expectedException)
        {
            Assert.That(e.Message, Does.Contain(exceptionContains));
            return;
        }

        TestsHelper.VerifyLoggerWasCalled(
            MockLogger,
            LogLevel.Critical,
            LoggerExtension.ResultEventId,
            Times.Once,
            outputContains);
    }
}
