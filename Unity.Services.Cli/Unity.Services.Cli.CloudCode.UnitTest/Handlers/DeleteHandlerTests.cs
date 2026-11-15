using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using Unity.Services.Cli.CloudCode.Handlers;
using Unity.Services.Cli.CloudCode.Input;
using Unity.Services.Cli.CloudCode.Service;
using Unity.Services.Cli.Common.Exceptions;
using Unity.Services.Cli.Common.Utils;
using Unity.Services.Gateway.CloudCodeApiV1.Generated.Client;

namespace Unity.Services.Cli.CloudCode.UnitTest.Handlers;

[TestFixture]
class DeleteHandlerTests
{
    readonly Mock<IUnityEnvironment> m_MockUnityEnvironment = new();
    readonly Mock<ICloudCodeService> m_MockCloudCode = new();
    readonly Mock<ILogger> m_MockLogger = new();

    [SetUp]
    public void SetUp()
    {
        m_MockUnityEnvironment.Reset();
        m_MockCloudCode.Reset();
        m_MockLogger.Reset();
    }

    [Test]
    public void DeleteAsync_ConflictExplainsTheScriptIsInARelease()
    {
        var conflict = new ApiException(409, "Conflict");
        m_MockCloudCode.Setup(ex => ex
                .DeleteAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(),
                    It.IsAny<CancellationToken>()))
            .ThrowsAsync(conflict);

        CloudCodeInput input = new()
        {
            CloudProjectId = "value",
            ScriptName = "my-script"
        };

        var ex = Assert.ThrowsAsync<CliException>(() => DeleteHandler.DeleteAsync(
            input,
            m_MockUnityEnvironment.Object,
            m_MockCloudCode.Object,
            m_MockLogger.Object,
            CancellationToken.None));

        Assert.That(ex!.Message, Does.Contain("'my-script'"));
        Assert.That(ex.Message, Does.Contain("assigned to a release"));
        Assert.That(ex.ExitCode, Is.EqualTo(ExitCode.HandledError));
        Assert.That(ex.InnerException, Is.SameAs(conflict));
    }

    [Test]
    public void DeleteAsync_OtherApiErrorsPropagateUnchanged()
    {
        var apiException = new ApiException(500, "Internal Server Error");
        m_MockCloudCode.Setup(ex => ex
                .DeleteAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(),
                    It.IsAny<CancellationToken>()))
            .ThrowsAsync(apiException);

        CloudCodeInput input = new()
        {
            CloudProjectId = "value",
            ScriptName = "my-script"
        };

        var ex = Assert.ThrowsAsync<ApiException>(() => DeleteHandler.DeleteAsync(
            input,
            m_MockUnityEnvironment.Object,
            m_MockCloudCode.Object,
            m_MockLogger.Object,
            CancellationToken.None));

        Assert.That(ex, Is.SameAs(apiException));
    }
}
