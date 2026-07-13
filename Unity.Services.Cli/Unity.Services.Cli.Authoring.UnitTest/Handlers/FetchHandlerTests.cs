using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using Spectre.Console;
using Unity.Services.Cli.Authoring.DeploymentDefinition;
using Unity.Services.Cli.Common.Console;
using Unity.Services.Cli.Common.Logging;
using Unity.Services.Cli.Common.Services;
using Unity.Services.Cli.Authoring.Handlers;
using Unity.Services.Cli.Authoring.Input;
using Unity.Services.Cli.Authoring.Model;
using Unity.Services.Cli.Authoring.Service;
using Unity.Services.Cli.Common.Telemetry.AnalyticEvent;
using Unity.Services.Cli.Common.Utils;
using Unity.Services.Cli.TestUtils;
using Unity.Services.Deployment.Core.Model;
using Unity.Services.DeploymentApi.Editor;

namespace Unity.Services.Cli.Authoring.UnitTest.Handlers;

[TestFixture]
public class FetchHandlerTests
{
    const string k_ValidEnvironmentId = "00000000-0000-0000-0000-000000000000";
    readonly Mock<IHost> m_Host = new();
    readonly Mock<ILogger> m_Logger = new();
    readonly Mock<IServiceProvider> m_ServiceProvider = new();
    readonly Mock<IFetchService> m_FetchService = new();
    readonly Mock<IAuthoringFileService> m_DdefService = new();
    readonly Mock<IAnalyticsEventBuilder> m_AnalyticsEventBuilder = new();
    readonly Mock<IUnityEnvironment> m_MockEnvironment = new();

    [SetUp]
    public void SetUp()
    {
        m_Host.Reset();
        m_ServiceProvider.Reset();
        m_FetchService.Reset();
        m_Logger.Reset();
        m_AnalyticsEventBuilder.Reset();
        m_MockEnvironment.Reset();

        m_MockEnvironment.Setup(x => x.FetchIdentifierAsync(CancellationToken.None)).Returns(Task.FromResult(k_ValidEnvironmentId));
        m_FetchService.Setup(s => s.ServiceName)
            .Returns("mock_test");
        m_FetchService.Setup(s => s.FileExtensions)
            .Returns(
            [
                ".test"
            ]);

        m_FetchService.Setup(
                s => s.FetchAsync(
                    It.IsAny<FetchInput>(),
                    It.IsAny<IReadOnlyList<AuthoringFile>>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<StatusContext?>(),
                    It.IsAny<CancellationToken>()))
            .Returns(
                Task.FromResult(
                    new FetchResult(
                        Array.Empty<IDeploymentItem>(),
                        Array.Empty<IDeploymentItem>(),
                        Array.Empty<IDeploymentItem>(),
                        Array.Empty<IDeploymentItem>(),
                        Array.Empty<IDeploymentItem>())));

        var bridge = new ServiceTypesBridge();
        var collection = bridge.CreateBuilder(new ServiceCollection());
        collection.AddScoped<IFetchService, TestFetchService>();
        collection.AddScoped<IFetchService>((_) => m_FetchService.Object);
        var provider = bridge.CreateServiceProvider(collection);

        m_Host.Setup(x => x.Services)
            .Returns(provider);

        m_DdefService
            .Setup(
                x => x.ResolveAuthoringFiles(
                    It.IsAny<IReadOnlyList<string>>(),
                    It.IsAny<IReadOnlyList<string>>()))
            .Returns(
                new DeploymentDefinitionFilteringResult(
                    new DeploymentDefinitionFiles(),
                    new Dictionary<string, IReadOnlyList<AuthoringFile>>
                    {
                        { ".test", new List<AuthoringFile> { new("path.test") }},
                        { ".test1", new List<AuthoringFile> { new("path1.test1") }}
                    },
                    new Dictionary<string, IDeploymentDefinition?>()));
    }

    class TestFetchService : IFetchService
    {
        string m_ServiceType = "Test";
        string m_ServiceName = "test";
        string m_DeployFileExtension = ".test";

        public string ServiceType => m_ServiceType;
        public string ServiceName => m_ServiceName;
        public IReadOnlyList<string> FileExtensions =>
        [
            m_DeployFileExtension
        ];

        public Task<FetchResult> FetchAsync(
            FetchInput input,
            IReadOnlyList<AuthoringFile> filePaths,
            string projectId,
            string environmentId,
            StatusContext? loadingContext,
            CancellationToken cancellationToken)
        {
            var res = new FetchResult(
                StringsToDeployContent(["updated1"]),
                StringsToDeployContent(["deleted1"]),
                Array.Empty<DeployContent>(),
                StringsToDeployContent(["file1"]),
                Array.Empty<DeployContent>());
            return Task.FromResult(res);
        }
    }

    [Test]
    public async Task FetchAsync_WithLoadingIndicator_CallsLoadingIndicatorStartLoading()
    {
        var mockLoadingIndicator = new Mock<ILoadingIndicator>();

        await FetchCommandHandler.FetchAsync(
            null!,
            null!,
            m_MockEnvironment.Object,
            null!,
            m_DdefService.Object,
            mockLoadingIndicator.Object,
            m_AnalyticsEventBuilder.Object,
            CancellationToken.None);

        mockLoadingIndicator.Verify(
            ex => ex.StartLoadingAsync(It.IsAny<string>(), It.IsAny<Func<StatusContext?, Task>>()), Times.Once);
    }

    [Test]
    [TestCase(true, Description = "Dry-Run is dry")]
    [TestCase(false, Description = "Wet-Run is wet")]
    public async Task FetchAsync_PrintsCorrectDryRun(bool dryRun)
    {
        var mockLogger = new Mock<ILogger>();
        var fetchInput = new FetchInput { DryRun = dryRun };
        var mockDdefService = new Mock<IAuthoringFileService>();

        await FetchCommandHandler.FetchAsync(
            m_Host.Object,
            fetchInput,
            m_MockEnvironment.Object,
            mockLogger.Object,
            (StatusContext?)null,
            m_DdefService.Object,
            m_AnalyticsEventBuilder.Object,
            CancellationToken.None);

        mockLogger.Verify(l => l.Log(
            LogLevel.Critical,
            LoggerExtension.ResultEventId,
            It.Is<object>(
                (o, t) => ((FetchResult)o).DryRun == dryRun),
            null,
            It.IsAny<Func<object, Exception?, string>>()));
    }


    [Test]
    public async Task FetchAsync_CallsGetServicesCorrectly()
    {
        var fetchInput = new FetchInput();

        await FetchCommandHandler.FetchAsync(
            m_Host.Object,
            fetchInput,
            m_MockEnvironment.Object,
            m_Logger.Object,
            (StatusContext)null!,
            m_DdefService.Object,
            m_AnalyticsEventBuilder.Object,
            CancellationToken.None);

        TestsHelper.VerifyLoggerWasCalled(m_Logger, LogLevel.Critical, LoggerExtension.ResultEventId, Times.Once);
    }

    static IReadOnlyList<DeployContent> StringsToDeployContent(IEnumerable<string> strs)
    {
        return strs.Select(s => new DeployContent(s, string.Empty, string.Empty)).ToList();
    }

    [Test]
    public void FetchAsync_ThrowsAggregateException()
    {
        m_Host.Reset();
        var bridge = new ServiceTypesBridge();
        var collection = bridge.CreateBuilder(new ServiceCollection());
        collection.AddScoped<IFetchService, TestFetchUnhandledExceptionFetchService>();
        var provider = bridge.CreateServiceProvider(collection);
        m_Host.Setup(x => x.Services).Returns(provider);
        var fetchInput = new FetchInput();
        Assert.ThrowsAsync<AggregateException>(async () =>
        {
            await FetchCommandHandler.FetchAsync(
                m_Host.Object,
                fetchInput,
                m_MockEnvironment.Object,
                m_Logger.Object,
                (StatusContext)null!,
                m_DdefService.Object,
                m_AnalyticsEventBuilder.Object,
                CancellationToken.None);
        });
    }

    [Test]
    public async Task FetchAsync_ReconcileWillNotExecutedWithNoServiceFlag()
    {
        var input = new FetchInput()
        {
            Reconcile = true
        };

        await FetchCommandHandler.FetchAsync(
            m_Host.Object,
            input,
            m_MockEnvironment.Object,
            m_Logger.Object,
            (StatusContext)null!,
            m_DdefService.Object,
            m_AnalyticsEventBuilder.Object,
            CancellationToken.None);

        m_FetchService.Verify(
            s => s.FetchAsync(
                It.IsAny<FetchInput>(),
                It.IsAny<List<AuthoringFile>>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<StatusContext?>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public async Task FetchAsync_ReconcileExecuteWithServiceFlag()
    {
        var input = new FetchInput()
        {
            Reconcile = true,
            Services = new[]
            {
                "mock_test"
            }
        };

        await FetchCommandHandler.FetchAsync(
            m_Host.Object,
            input,
            m_MockEnvironment.Object,
            m_Logger.Object,
            (StatusContext)null!,
            m_DdefService.Object,
            m_AnalyticsEventBuilder.Object,
            CancellationToken.None);

        m_FetchService.Verify(
            s => s.FetchAsync(
                It.IsAny<FetchInput>(),
                It.IsAny<IReadOnlyList<AuthoringFile>>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<StatusContext?>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task FetchAsync_ExecuteWithCorrectServiceFlag()
    {
        var input = new FetchInput()
        {
            Services = new[]
            {
                "mock_test"
            }
        };

        await FetchCommandHandler.FetchAsync(
            m_Host.Object,
            input,
            m_MockEnvironment.Object,
            m_Logger.Object,
            (StatusContext)null!,
            m_DdefService.Object,
            m_AnalyticsEventBuilder.Object,
            CancellationToken.None);

        m_FetchService.Verify(
            s => s.FetchAsync(
                It.IsAny<FetchInput>(),
                It.IsAny<IReadOnlyList<AuthoringFile>>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<StatusContext?>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task FetchAsync_NotExecuteWithIncorrectServiceFlag()
    {
        var input = new FetchInput()
        {
            Services = new[]
            {
                "non-test"
            }
        };

        await FetchCommandHandler.FetchAsync(
            m_Host.Object,
            input,
            m_MockEnvironment.Object,
            m_Logger.Object,
            (StatusContext)null!,
            m_DdefService.Object,
            m_AnalyticsEventBuilder.Object,
            CancellationToken.None);

        m_FetchService.Verify(
            s => s.FetchAsync(
                It.IsAny<FetchInput>(),
                It.IsAny<List<AuthoringFile>>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<StatusContext?>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public async Task FetchAsync_MultipleDeploymentDefinitionsException_NotExecuted()
    {
        var input = new FetchInput()
        {
            Services = new[]
            {
                "mock_test"
            }
        };

        m_DdefService
            .Setup(
                s => s.ResolveAuthoringFiles(
                    It.IsAny<IReadOnlyList<string>>(),
                    It.IsAny<IReadOnlyList<string>>()))
            .Throws(
                () =>
                    new MultipleDeploymentDefinitionInDirectoryException(
                        new Mock<IDeploymentDefinition>().Object,
                        new Mock<IDeploymentDefinition>().Object,
                        "path"));

        await FetchCommandHandler.FetchAsync(
            m_Host.Object,
            input,
            m_MockEnvironment.Object,
            m_Logger.Object,
            (StatusContext)null!,
            m_DdefService.Object,
            m_AnalyticsEventBuilder.Object,
            CancellationToken.None);

        m_FetchService.Verify(
            s => s.FetchAsync(
                It.IsAny<FetchInput>(),
                It.IsAny<List<AuthoringFile>>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<StatusContext?>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public async Task FetchAsync_DeploymentDefinitionIntersectionException_NotExecuted()
    {
        var input = new FetchInput()
        {
            Services = new[]
            {
                "mock_test"
            }
        };

        m_DdefService
            .Setup(
                s => s.ResolveAuthoringFiles(
                    It.IsAny<IReadOnlyList<string>>(),
                    It.IsAny<IReadOnlyList<string>>()))
            .Throws(
                new DeploymentDefinitionFileIntersectionException(
                    new Dictionary<IDeploymentDefinition, List<string>>(),
                    true));

        await FetchCommandHandler.FetchAsync(
            m_Host.Object,
            input,
            m_MockEnvironment.Object,
            m_Logger.Object,
            (StatusContext)null!,
            m_DdefService.Object,
            m_AnalyticsEventBuilder.Object,
            CancellationToken.None);

        m_FetchService.Verify(
            s => s.FetchAsync(
                It.IsAny<FetchInput>(),
                It.IsAny<List<AuthoringFile>>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<StatusContext?>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public async Task FetchAsync_DeploymentDefinitionsHaveExclusion_ExclusionsLogged()
    {
        var input = new FetchInput()
        {
            Services = new[]
            {
                "mock_test"
            }
        };

        var mockResult = new Mock<IDeploymentDefinitionFilteringResult>();
        mockResult
            .Setup(r => r.AllFilesByExtension)
            .Returns(
                new Dictionary<string, IReadOnlyList<AuthoringFile>>
                {
                    { ".test", new List<AuthoringFile>() }
                });
        var mockFiles = new Mock<IDeploymentDefinitionFiles>();
        mockFiles
            .Setup(f => f.HasExcludes)
            .Returns(true);
        mockResult
            .Setup(r => r.DefinitionFiles)
            .Returns(mockFiles.Object);
        m_DdefService
            .Setup(
                s => s.ResolveAuthoringFiles(
                    It.IsAny<IReadOnlyList<string>>(),
                    It.IsAny<IReadOnlyList<string>>()))
            .Returns(mockResult.Object);


        await FetchCommandHandler.FetchAsync(
            m_Host.Object,
            input,
            m_MockEnvironment.Object,
            m_Logger.Object,
            (StatusContext)null!,
            m_DdefService.Object,
            m_AnalyticsEventBuilder.Object,
            CancellationToken.None);

        mockResult.Verify(r => r.GetExclusionsLogMessage(), Times.Once);
    }

    [Test]
    public async Task FetchAsync_ReconcileWithDdef_NotExecuted()
    {
        var input = new FetchInput()
        {
            Services = new[]
            {
                "mock_test"
            },
            Path = "some/path/to/A.ddef",
            Reconcile = true
        };

        await FetchCommandHandler.FetchAsync(
            m_Host.Object,
            input,
            m_MockEnvironment.Object,
            m_Logger.Object,
            (StatusContext)null!,
            m_DdefService.Object,
            m_AnalyticsEventBuilder.Object,
            CancellationToken.None);

        m_FetchService.Verify(
            s => s.FetchAsync(
                It.IsAny<FetchInput>(),
                It.IsAny<List<AuthoringFile>>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<StatusContext?>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    class TestFetchUnhandledExceptionFetchService : IFetchService
    {
        string m_ServiceType = "Test";
        string m_ServiceName = "test";
        string m_DeployFileExtension = ".test";

        public string ServiceType => m_ServiceType;
        public string ServiceName => m_ServiceName;
        public IReadOnlyList<string> FileExtensions => new[]
        {
            m_DeployFileExtension
        };

        public Task<FetchResult> FetchAsync(
            FetchInput input,
            IReadOnlyList<AuthoringFile> filePaths,
            string projectId,
            string environmentId,
            StatusContext? loadingContext,
            CancellationToken cancellationToken)
        {
            return Task.FromException<FetchResult>(new NullReferenceException());
        }
    }

    [Test]
    public async Task FetchAsync_WithSingleDdef_FiltersOutNestedDdefFiles()
    {
        // Arrange
        var input = new FetchInput
        {
            Services = new[] { "mock_test" },
            Path = "player/"
        };

        // Setup: Create root and nested deployment definitions
        var rootDdefPath = Path.GetFullPath("player/player.ddef");
        var nestedDdefPath = Path.GetFullPath("player/ios/ios.ddef");

        var rootDdef = new Mock<IDeploymentDefinition>();
        rootDdef.Setup(d => d.Path).Returns(rootDdefPath);
        rootDdef.Setup(d => d.AdditionalProperties).Returns(
            new Dictionary<string, object> { ["variantTags"] = new[] { "player", "xbox" } });

        var nestedDdef = new Mock<IDeploymentDefinition>();
        nestedDdef.Setup(d => d.Path).Returns(nestedDdefPath);
        nestedDdef.Setup(d => d.AdditionalProperties).Returns(
            new Dictionary<string, object> { ["variantTags"] = new[] { "player", "ios" } });

        // Create authoring files with different ddef paths
        var authoringFiles = new List<AuthoringFile>
        {
            new AuthoringFile("player/asset.test", rootDdef.Object),
            new AuthoringFile("player/coord.test", rootDdef.Object),
            new AuthoringFile("player/ios/coord.test", nestedDdef.Object)
        };

        var ddefResult = new DeploymentDefinitionFilteringResult(
            new DeploymentDefinitionFiles(),
            new Dictionary<string, IReadOnlyList<AuthoringFile>>
            {
                { ".test", authoringFiles }
            },
            new Dictionary<string, IDeploymentDefinition?>
            {
                { "player/", rootDdef.Object }
            });

        m_DdefService
            .Setup(s => s.ResolveAuthoringFiles(
                It.IsAny<IReadOnlyList<string>>(),
                It.IsAny<IReadOnlyList<string>>()))
            .Returns(ddefResult);

        // Act
        await FetchCommandHandler.FetchAsync(
            m_Host.Object,
            input,
            m_MockEnvironment.Object,
            m_Logger.Object,
            (StatusContext?)null,
            m_DdefService.Object,
            m_AnalyticsEventBuilder.Object,
            CancellationToken.None);

        // Assert: only files from root ddef were passed to the fetch service
        m_FetchService.Verify(
            s => s.FetchAsync(
                It.IsAny<FetchInput>(),
                It.Is<IReadOnlyList<AuthoringFile>>(files =>
                    files.Count == 2 &&
                    files.Any(f => f.Path == "player/asset.test") &&
                    files.Any(f => f.Path == "player/coord.test") &&
                    !files.Any(f => f.Path == "player/ios/coord.test")),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<StatusContext?>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task FetchAsync_WithInputVariantTags_OverwritesTargetTags()
    {
        // Arrange
        var input = new FetchInput
        {
            Services = new[] { "mock_test" },
            Path = "player/",
            VariantTags = new List<string> { "player", "ps5" },
            UseForce = true
        };

        var rootDdefPath = Path.GetFullPath("player/player.ddef");
        var rootDdef = new Mock<IDeploymentDefinition>();
        rootDdef.Setup(d => d.Path).Returns(rootDdefPath);
        rootDdef.Setup(d => d.AdditionalProperties).Returns(
            new Dictionary<string, object> { ["variantTags"] = new[] { "player", "xbox" } });

        var authoringFiles = new List<AuthoringFile>
        {
            new AuthoringFile("player/asset.test", rootDdef.Object)
        };

        var ddefResult = new DeploymentDefinitionFilteringResult(
            new DeploymentDefinitionFiles(),
            new Dictionary<string, IReadOnlyList<AuthoringFile>>
            {
                { ".test", authoringFiles }
            },
            new Dictionary<string, IDeploymentDefinition?>
            {
                { "player/", rootDdef.Object }
            });

        m_DdefService
            .Setup(s => s.ResolveAuthoringFiles(
                It.IsAny<IReadOnlyList<string>>(),
                It.IsAny<IReadOnlyList<string>>()))
            .Returns(ddefResult);

        // Act
        await FetchCommandHandler.FetchAsync(
            m_Host.Object,
            input,
            m_MockEnvironment.Object,
            m_Logger.Object,
            (StatusContext?)null,
            m_DdefService.Object,
            m_AnalyticsEventBuilder.Object,
            CancellationToken.None);

        // Assert: files were passed with overwritten variant tags
        m_FetchService.Verify(
            s => s.FetchAsync(
                It.Is<FetchInput>(i => i.VariantTags != null && i.VariantTags.SequenceEqual(new[] { "player", "ps5" })),
                It.Is<IReadOnlyList<AuthoringFile>>(files =>
                    files.Count == 1 &&
                    files[0].VariantTags.SequenceEqual(new[] { "player", "ps5" })),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<StatusContext?>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task FetchAsync_WithoutInputVariantTags_UsesTargetDdefTags()
    {
        // Arrange
        var input = new FetchInput
        {
            Services = new[] { "mock_test" },
            Path = "player/"
        };

        var rootDdefPath = Path.GetFullPath("player/player.ddef");
        var rootDdef = new Mock<IDeploymentDefinition>();
        rootDdef.Setup(d => d.Path).Returns(rootDdefPath);
        rootDdef.Setup(d => d.AdditionalProperties).Returns(
            new Dictionary<string, object> { ["variantTags"] = new[] { "player", "xbox" } });

        var authoringFiles = new List<AuthoringFile>
        {
            new AuthoringFile("player/asset.test", rootDdef.Object)
        };

        var ddefResult = new DeploymentDefinitionFilteringResult(
            new DeploymentDefinitionFiles(),
            new Dictionary<string, IReadOnlyList<AuthoringFile>>
            {
                { ".test", authoringFiles }
            },
            new Dictionary<string, IDeploymentDefinition?>
            {
                { "player/", rootDdef.Object }
            });

        m_DdefService
            .Setup(s => s.ResolveAuthoringFiles(
                It.IsAny<IReadOnlyList<string>>(),
                It.IsAny<IReadOnlyList<string>>()))
            .Returns(ddefResult);

        // Act
        await FetchCommandHandler.FetchAsync(
            m_Host.Object,
            input,
            m_MockEnvironment.Object,
            m_Logger.Object,
            (StatusContext?)null,
            m_DdefService.Object,
            m_AnalyticsEventBuilder.Object,
            CancellationToken.None);

        // Assert: input variant tags were populated with ddef tags
        m_FetchService.Verify(
            s => s.FetchAsync(
                It.Is<FetchInput>(i =>
                    i.VariantTags != null &&
                    i.VariantTags.SequenceEqual(new[] { "player", "xbox" })),
                It.IsAny<IReadOnlyList<AuthoringFile>>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<StatusContext?>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task FetchAsync_WithNoDdefForInputPath_UsesEmptyVariantTags()
    {
        // Arrange
        var input = new FetchInput
        {
            Services = new[] { "mock_test" },
            Path = "player/"
        };

        var authoringFiles = new List<AuthoringFile>
        {
            new AuthoringFile("player/asset.test")
        };

        var ddefResult = new DeploymentDefinitionFilteringResult(
            new DeploymentDefinitionFiles(),
            new Dictionary<string, IReadOnlyList<AuthoringFile>>
            {
                { ".test", authoringFiles }
            },
            new Dictionary<string, IDeploymentDefinition?>
            {
                { "player/", null }
            });

        m_DdefService
            .Setup(s => s.ResolveAuthoringFiles(
                It.IsAny<IReadOnlyList<string>>(),
                It.IsAny<IReadOnlyList<string>>()))
            .Returns(ddefResult);

        // Act
        await FetchCommandHandler.FetchAsync(
            m_Host.Object,
            input,
            m_MockEnvironment.Object,
            m_Logger.Object,
            (StatusContext?)null,
            m_DdefService.Object,
            m_AnalyticsEventBuilder.Object,
            CancellationToken.None);

        // Assert: input variant tags are empty
        m_FetchService.Verify(
            s => s.FetchAsync(
                It.Is<FetchInput>(i =>
                    i.VariantTags != null &&
                    i.VariantTags.Count == 0),
                It.IsAny<IReadOnlyList<AuthoringFile>>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<StatusContext?>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task FetchAsync_WithMultipleDdefsInResult_OnlyProcessesMatchingOne()
    {
        var input = new FetchInput
        {
            Services = new[] { "mock_test" },
            Path = "player/"
        };

        // Setup: Three different ddefs
        var rootDdefPath = Path.GetFullPath("player/player.ddef");
        var iosDdefPath = Path.GetFullPath("player/ios/ios.ddef");
        var androidDdefPath = Path.GetFullPath("player/android/android.ddef");

        var rootDdef = new Mock<IDeploymentDefinition>();
        rootDdef.Setup(d => d.Path).Returns(rootDdefPath);
        rootDdef.Setup(d => d.AdditionalProperties).Returns(
            new Dictionary<string, object> { ["variantTags"] = new[] { "player", "xbox" } });

        var iosDdef = new Mock<IDeploymentDefinition>();
        iosDdef.Setup(d => d.Path).Returns(iosDdefPath);
        iosDdef.Setup(d => d.AdditionalProperties).Returns(
            new Dictionary<string, object> { ["variantTags"] = new[] { "player", "ios" } });

        var androidDdef = new Mock<IDeploymentDefinition>();
        androidDdef.Setup(d => d.Path).Returns(androidDdefPath);
        androidDdef.Setup(d => d.AdditionalProperties).Returns(
            new Dictionary<string, object> { ["variantTags"] = new[] { "player", "android" } });

        var authoringFiles = new List<AuthoringFile>
        {
            new AuthoringFile("player/asset.test", rootDdef.Object),
            new AuthoringFile("player/coord.test", rootDdef.Object),
            new AuthoringFile("player/ios/ios_coord.test", iosDdef.Object),
            new AuthoringFile("player/android/android_coord.test", androidDdef.Object)
        };

        var ddefResult = new DeploymentDefinitionFilteringResult(
            new DeploymentDefinitionFiles(),
            new Dictionary<string, IReadOnlyList<AuthoringFile>>
            {
                { ".test", authoringFiles }
            },
            new Dictionary<string, IDeploymentDefinition?>
            {
                { "player/", rootDdef.Object }
            });

        m_DdefService
            .Setup(s => s.ResolveAuthoringFiles(
                It.IsAny<IReadOnlyList<string>>(),
                It.IsAny<IReadOnlyList<string>>()))
            .Returns(ddefResult);

        await FetchCommandHandler.FetchAsync(
            m_Host.Object,
            input,
            m_MockEnvironment.Object,
            m_Logger.Object,
            (StatusContext?)null,
            m_DdefService.Object,
            m_AnalyticsEventBuilder.Object,
            CancellationToken.None);

        // Verify only root ddef files are processed
        m_FetchService.Verify(
            s => s.FetchAsync(
                It.IsAny<FetchInput>(),
                It.Is<IReadOnlyList<AuthoringFile>>(files =>
                    files.Count == 2 &&
                    files.Any(f => f.Path == "player/asset.test") &&
                    files.Any(f => f.Path == "player/coord.test") &&
                    !files.Any(f => f.Path == "player/ios/ios_coord.test") &&
                    !files.Any(f => f.Path == "player/android/android_coord.test")),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<StatusContext?>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task FetchAsync_WithFilesWithoutDdef_HandledCorrectly()
    {
        var input = new FetchInput
        {
            Services = new[] { "mock_test" },
            Path = "player/"
        };

        var rootDdefPath = Path.GetFullPath("player/player.ddef");
        var rootDdef = new Mock<IDeploymentDefinition>();
        rootDdef.Setup(d => d.Path).Returns(rootDdefPath);
        rootDdef.Setup(d => d.AdditionalProperties).Returns(
            new Dictionary<string, object> { ["variantTags"] = new[] { "player", "xbox" } });

        // Mix of files with and without ddef
        var authoringFiles = new List<AuthoringFile>
        {
            new AuthoringFile("player/asset.test", rootDdef.Object),
            new AuthoringFile("player/standalone.test") // No ddef
        };

        var ddefResult = new DeploymentDefinitionFilteringResult(
            new DeploymentDefinitionFiles(),
            new Dictionary<string, IReadOnlyList<AuthoringFile>>
            {
                { ".test", authoringFiles }
            },
            new Dictionary<string, IDeploymentDefinition?>
            {
                { "player/", rootDdef.Object }
            });

        m_DdefService
            .Setup(s => s.ResolveAuthoringFiles(
                It.IsAny<IReadOnlyList<string>>(),
                It.IsAny<IReadOnlyList<string>>()))
            .Returns(ddefResult);

        // Act
        await FetchCommandHandler.FetchAsync(
            m_Host.Object,
            input,
            m_MockEnvironment.Object,
            m_Logger.Object,
            (StatusContext?)null,
            m_DdefService.Object,
            m_AnalyticsEventBuilder.Object,
            CancellationToken.None);

        // Assert: only files with matching ddef are processed
        m_FetchService.Verify(
            s => s.FetchAsync(
                It.IsAny<FetchInput>(),
                It.Is<IReadOnlyList<AuthoringFile>>(files =>
                    files.Count == 1 &&
                    files[0].Path == "player/asset.test"),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<StatusContext?>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
