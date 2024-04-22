using Microsoft.Extensions.Logging;
using Moq;
using Unity.Services.Cli.Common;
using Unity.Services.Cli.Common.Console;
using Unity.Services.Cli.Common.Telemetry.AnalyticEvent;
using Unity.Services.Cli.Environment;

namespace Unity.Services.Cli.TestUtils;

public class MockHelper
{
    public readonly Mock<IConfigurationService> MockConfiguration = new();
    public readonly Mock<IEnvironmentService> MockEnvironment = new();
    public readonly Mock<IConsoleTable> MockConsoleTable = new();
    public readonly Mock<ILogger> MockLogger = new();
    public readonly Mock<IAnalyticEvent> MockDiagnostics = new();

    public void ClearInvocations()
    {
        MockConfiguration.Reset();
        MockEnvironment.Reset();
        MockLogger.Reset();
        MockDiagnostics.Reset();
        MockConsoleTable.Reset();
    }
}
