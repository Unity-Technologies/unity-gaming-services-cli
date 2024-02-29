using Microsoft.Extensions.Logging;
using Moq;
using Unity.Services.Cli.Common.Exceptions;
using Unity.Services.Cli.GameServerHosting.Handlers;
using Unity.Services.Cli.GameServerHosting.Input;
using Unity.Services.Cli.TestUtils;

namespace Unity.Services.Cli.GameServerHosting.UnitTest.Handlers;

[TestFixture]
[TestOf(typeof(CoreDumpDeleteHandler))]
class CoreDumpDeleteHandlerTest : HandlerCommon
{
    [TestCase(
        ValidProjectId,
        ValidEnvironmentName,
        CoreDumpMockFleetIdWithEnabledConfig,
        "Core Dump config deleted successfully",
        TestName = "Core Dump Config should be deleted")]
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
        TestName = "Invalid Project Id, unexpected exception")]
    [TestCase(
        ValidProjectId,
        ValidEnvironmentName,
        null,
        "Core Dump config deleted successfully",
        true,
        "Missing value for input: 'fleet-id'",
        TestName = "Missing input")]
    public async Task CoreDumpDeleteAsync(
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
            await CoreDumpDeleteHandler.CoreDumpDeleteAsync(
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
            LogLevel.Information,
            0,
            Times.Once,
            outputContains);
    }
}
