using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using Spectre.Console;
using Unity.Services.Cli.Common.Console;
using Unity.Services.Cli.Common.Logging;
using Unity.Services.Cli.Common.Services;
using Unity.Services.Cli.Authoring.Handlers;
using Unity.Services.Cli.Authoring.Input;
using Unity.Services.Cli.Authoring.Model;
using Unity.Services.Cli.Authoring.Service;
using Unity.Services.Cli.TestUtils;

namespace Unity.Services.Cli.Authoring.UnitTest.Handlers;

[TestFixture]
public class FetchHandlerTests
{
    readonly Mock<IHost> m_Host = new();
    readonly Mock<ILogger> m_Logger = new();
    readonly Mock<IServiceProvider> m_ServiceProvider = new();
    readonly Mock<IDeploymentService> m_DeploymentService = new();

    class TestFetchService : IFetchService
    {
        string m_ServiceType = "Test";
        string m_DeployFileExtension = ".test";

        string IFetchService.ServiceType => m_ServiceType;

        string IFetchService.FileExtension => m_DeployFileExtension;

        public Task<FetchResult> FetchAsync(FetchInput input, StatusContext? loadingContext, CancellationToken cancellationToken)
        {
            var res = new FetchResult(
                new[] { "updated1" },
                new[] { "deleted1" },
                Array.Empty<string>(),
                new[] { "file1" },
                Array.Empty<string>());
            return Task.FromResult(res);
        }
    }

    [SetUp]
    public void SetUp()
    {
        m_Host.Reset();
        m_ServiceProvider.Reset();
        m_DeploymentService.Reset();
        m_Logger.Reset();

        var bridge = new ServiceTypesBridge();
        var collection = bridge.CreateBuilder(new ServiceCollection());
        collection.AddScoped<IFetchService, TestFetchService>();
        var provider = bridge.CreateServiceProvider(collection);

        m_Host.Setup(x => x.Services)
            .Returns(provider);
    }

    [Test]
    public async Task FetchAsync_WithLoadingIndicator_CallsLoadingIndicatorStartLoading()
    {
        var mockLoadingIndicator = new Mock<ILoadingIndicator>();

        await FetchHandler.FetchAsync(
            null!, null!, null!, mockLoadingIndicator.Object, CancellationToken.None);

        mockLoadingIndicator.Verify(
            ex => ex.StartLoadingAsync(It.IsAny<string>(), It.IsAny<Func<StatusContext?, Task>>()), Times.Once);
    }


    [Test]
    public async Task FetchAsync_CallsGetServicesCorrectly()
    {
        var fetchInput = new FetchInput();

        await FetchHandler.FetchAsync(
            m_Host.Object, fetchInput, m_Logger.Object, (StatusContext)null!, CancellationToken.None);

        TestsHelper.VerifyLoggerWasCalled(m_Logger, LogLevel.Critical, LoggerExtension.ResultEventId, Times.Once);
    }
}
