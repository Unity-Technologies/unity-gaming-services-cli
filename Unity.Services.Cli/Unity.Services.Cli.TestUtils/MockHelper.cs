using Microsoft.Extensions.Logging;
using Moq;
using Unity.Services.Cli.Common;
using Unity.Services.Cli.Common.Telemetry;
using Unity.Services.Cli.Environment;

namespace Unity.Services.Cli.TestUtils;

public class MockHelper
{
    public readonly Mock<IConfigurationService> MockConfiguration = new();
    public readonly Mock<IEnvironmentService> MockEnvironment = new();
    public readonly Mock<ILogger> MockLogger = new();
    public readonly Mock<IDiagnostics> MockDiagnostics = new();

    public void ClearInvocations()
    {
        MockConfiguration.Invocations.Clear();
        MockEnvironment.Invocations.Clear();
        MockLogger.Invocations.Clear();
        MockDiagnostics.Invocations.Clear();
    }
}
