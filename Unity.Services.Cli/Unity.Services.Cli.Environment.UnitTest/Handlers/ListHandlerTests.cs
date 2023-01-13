using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using Spectre.Console;
using Unity.Services.Cli.Common.Console;
using Unity.Services.Cli.Common.Exceptions;
using Unity.Services.Cli.Common.Logging;
using Unity.Services.Cli.Environment.Handlers;
using Unity.Services.Cli.Environment.Input;
using Unity.Services.Cli.TestUtils;
using Unity.Services.Gateway.IdentityApiV1.Generated.Model;
using Models = Unity.Services.Cli.Common.Models;

namespace Unity.Services.Cli.Environment.UnitTest.Handlers;

[TestFixture]
class ListHandlerTests
{
    const string k_ValidProjectId = "00000000-0000-0000-0000-000000000000";

    readonly MockHelper m_MockHelper = new();

    [SetUp]
    public void SetUp()
    {
        m_MockHelper.ClearInvocations();
        m_MockHelper.MockConfiguration
            .Setup(c => c.GetConfigArgumentsAsync(Models.Keys.ConfigKeys.ProjectId, CancellationToken.None))
            .Returns(Task.FromResult(k_ValidProjectId)!);
    }

    [Test]
    public async Task LoadListAsync_CallsLoadingIndicatorStartLoading()
    {
        Mock<ILoadingIndicator> mockLoadingIndicator = new Mock<ILoadingIndicator>();
        var input = new EnvironmentInput
        {
            CloudProjectId = null
        };

        await ListHandler.ListAsync(input, m_MockHelper.MockEnvironment.Object, m_MockHelper.MockLogger.Object,
            mockLoadingIndicator.Object, CancellationToken.None);

        mockLoadingIndicator.Verify(ex => ex
            .StartLoadingAsync(It.IsAny<string>(), It.IsAny<Func<StatusContext?,Task>>()), Times.Once);
    }

    [Test]
    public void ListAsync_NullProjectIdPrintsError()
    {
        var input = new EnvironmentInput
        {
            CloudProjectId = null
        };

        Assert.ThrowsAsync<MissingConfigurationException>(() =>
            ListHandler.ListAsync(
                input,
                m_MockHelper.MockEnvironment.Object,
                m_MockHelper.MockLogger.Object,
                CancellationToken.None
            ));

        m_MockHelper.MockEnvironment.Verify(
            api =>
                api.ListAsync(
                    input.CloudProjectId!,
                    It.IsAny<CancellationToken>()),
            Times.Never);

        TestsHelper.VerifyLoggerWasCalled(m_MockHelper.MockLogger,
            LogLevel.Critical, LoggerExtension.ResultEventId, Times.Never);
    }

    [TestCase(k_ValidProjectId)]
    public async Task ListAsync_ProjectIdOptionDoNotRunConfiguration(string projectId)
    {
        var input = new EnvironmentInput
        {
            CloudProjectId = projectId
        };

        await ListHandler.ListAsync(input,
            m_MockHelper.MockEnvironment.Object, m_MockHelper.MockLogger.Object, CancellationToken.None);

        Assert.AreEqual(0, m_MockHelper.MockConfiguration.Invocations.Count);
        m_MockHelper.MockEnvironment.Verify(e =>
            e.ListAsync(input.CloudProjectId, CancellationToken.None));
        TestsHelper.VerifyLoggerWasCalled(m_MockHelper.MockLogger, LogLevel.Critical, LoggerExtension.ResultEventId);
    }

    [Test]
    public async Task ListAsync_JsonOptionLogsResultInCorrectFormat()
    {
        var input = new EnvironmentInput
        {
            CloudProjectId = k_ValidProjectId,
            IsJson = true
        };

        EnvironmentResponse[] response = new EnvironmentResponse[1];

        m_MockHelper.MockEnvironment.Setup(ex => ex
            .ListAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        await ListHandler.ListAsync(input,
            m_MockHelper.MockEnvironment.Object, m_MockHelper.MockLogger.Object, CancellationToken.None);

        TestsHelper.VerifyLoggerWasCalled(m_MockHelper.MockLogger, LogLevel.Critical, LoggerExtension.ResultEventId,
            Times.Once, response.ToString());
    }
}
