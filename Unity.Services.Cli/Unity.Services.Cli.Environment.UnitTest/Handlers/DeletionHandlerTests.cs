using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using Spectre.Console;
using Unity.Services.Cli.Common.Console;
using Unity.Services.Cli.Common.Utils;
using Unity.Services.Cli.Environment.Handlers;
using Unity.Services.Cli.Environment.Input;
using Unity.Services.Cli.TestUtils;
using Unity.Services.Gateway.IdentityApiV1.Generated.Model;
using Models = Unity.Services.Cli.Common.Models;

namespace Unity.Services.Cli.Environment.UnitTest.Handlers;

class DeletionHandlerTests
{
    const string k_ValidProjectId = "00000000-0000-0000-0000-000000000000";
    const string k_ValidEnvironmentName = "staging";

    readonly MockHelper m_MockHelper = new();
    readonly Mock<IUnityEnvironment> m_MockUnityEnvironment = new();

    [SetUp]
    public void SetUp()
    {
        m_MockHelper.ClearInvocations();
        m_MockUnityEnvironment.Reset();
        m_MockHelper.MockConfiguration
            .Setup(c => c.GetConfigArgumentsAsync(Models.Keys.ConfigKeys.ProjectId, CancellationToken.None))
            .Returns(Task.FromResult(k_ValidProjectId)!);
    }

    [Test]
    public async Task LoadDeleteAsync_CallsLoadingIndicatorStartLoading()
    {
        Mock<ILoadingIndicator> mockLoadingIndicator = new Mock<ILoadingIndicator>();

        var input = new EnvironmentInput
        {
            CloudProjectId = null
        };

        await DeletionHandler.DeleteAsync(input, m_MockHelper.MockEnvironment.Object, m_MockHelper.MockLogger.Object,
            mockLoadingIndicator.Object, m_MockUnityEnvironment.Object, CancellationToken.None);

        mockLoadingIndicator.Verify(ex => ex
            .StartLoadingAsync(It.IsAny<string>(), It.IsAny<Func<StatusContext?, Task>>()), Times.Once);
    }

    [Test]
    public async Task DeleteAsync_ProjectIdOptionDoNotRunConfiguration()
    {
        string mockEnvironmentId = "00000000-0000-0000-0000-000000000000";
        IEnumerable<EnvironmentResponse> responses = new[]
        {
            new EnvironmentResponse()
            {
                Name = k_ValidEnvironmentName,
                Id = new Guid(mockEnvironmentId)
            }
        };

        var input = new EnvironmentInput
        {
            CloudProjectId = k_ValidProjectId,
            EnvironmentName = k_ValidEnvironmentName,
        };

        m_MockUnityEnvironment.Setup(c =>
                c.FetchIdentifierFromSpecificEnvironmentNameAsync(k_ValidEnvironmentName, CancellationToken.None))
            .ReturnsAsync(mockEnvironmentId);

        await DeletionHandler.DeleteAsync(input, m_MockHelper.MockEnvironment.Object, m_MockHelper.MockLogger.Object,
            m_MockUnityEnvironment.Object, CancellationToken.None);

        Assert.AreEqual(0, m_MockHelper.MockConfiguration.Invocations.Count);
        m_MockHelper.MockEnvironment.Verify(e =>
            e.DeleteAsync(input.CloudProjectId, mockEnvironmentId, CancellationToken.None), Times.Once);

        TestsHelper.VerifyLoggerWasCalled(m_MockHelper.MockLogger, LogLevel.Information);
    }
}
