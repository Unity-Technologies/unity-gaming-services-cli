using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using Spectre.Console;
using Unity.Services.Cli.Authoring.DeploymentDefinition;
using Unity.Services.Cli.Authoring.Handlers;
using Unity.Services.Cli.Authoring.Input;
using Unity.Services.Cli.Authoring.Model;
using Unity.Services.Cli.Authoring.Service;
using Unity.Services.Cli.Common.Services;
using Unity.Services.Cli.Common.Telemetry.AnalyticEvent;
using Unity.Services.Cli.Common.Utils;
using Unity.Services.Deployment.Core.Model;
using Unity.Services.DeploymentApi.Editor;

namespace Unity.Services.Cli.Authoring.UnitTest.Handlers;

/// <summary>
/// Tests that verify the behavioral differences between FetchCommandHandler and
/// DeployCommandHandler around how they handle multiple deployment definitions.
/// </summary>
[TestFixture]
public class FetchVsDeployHandlerComparisonTests
{
    const string k_ValidEnvironmentId = "00000000-0000-0000-0000-000000000000";
    readonly Mock<IHost> m_Host = new();
    readonly Mock<ILogger> m_Logger = new();
    readonly Mock<IFetchService> m_FetchService = new();
    readonly Mock<IDeploymentService> m_DeploymentService = new();
    readonly Mock<IAuthoringFileService> m_DdefService = new();
    readonly Mock<IAnalyticsEventBuilder> m_AnalyticsEventBuilder = new();
    readonly Mock<IUnityEnvironment> m_MockEnvironment = new();
    readonly ServiceTypesBridge m_Bridge = new();

    [SetUp]
    public void SetUp()
    {
        m_Host.Reset();
        m_FetchService.Reset();
        m_DeploymentService.Reset();
        m_Logger.Reset();
        m_AnalyticsEventBuilder.Reset();
        m_MockEnvironment.Reset();
        m_DdefService.Reset();

        m_MockEnvironment.Setup(x => x.FetchIdentifierAsync(CancellationToken.None))
            .Returns(Task.FromResult(k_ValidEnvironmentId));

        // Setup fetch service
        m_FetchService.Setup(s => s.ServiceName).Returns("test");
        m_FetchService.Setup(s => s.ServiceType).Returns("Test");
        m_FetchService.Setup(s => s.FileExtensions).Returns(new[] { ".test" });
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

        // Setup deployment service
        m_DeploymentService.Setup(s => s.ServiceName).Returns("test");
        m_DeploymentService.Setup(s => s.ServiceType).Returns("Test");
        m_DeploymentService.Setup(s => s.FileExtensions).Returns(new[] { ".test" });
        m_DeploymentService.Setup(
                s => s.Deploy(
                    It.IsAny<DeployInput>(),
                    It.IsAny<IReadOnlyList<AuthoringFile>>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<StatusContext?>(),
                    It.IsAny<CancellationToken>()))
            .Returns(
                Task.FromResult(
                    new DeploymentResult(
                        Array.Empty<IDeploymentItem>(),
                        Array.Empty<IDeploymentItem>(),
                        Array.Empty<IDeploymentItem>(),
                        Array.Empty<IDeploymentItem>(),
                        Array.Empty<IDeploymentItem>())));

        // Setup host with services
        var collection = m_Bridge.CreateBuilder(new ServiceCollection());
        collection.AddScoped<IFetchService>((_) => m_FetchService.Object);
        collection.AddScoped<IDeploymentService>((_) => m_DeploymentService.Object);
        var provider = m_Bridge.CreateServiceProvider(collection);
        m_Host.Setup(x => x.Services).Returns(provider);
    }

    /// <summary>
    /// This test documents and verifies the key behavioral difference between Fetch and Deploy:
    ///
    /// - Fetch: When given an input path with an associated deployment definition, it filters
    ///   authoring files to ONLY process those that belong to that specific deployment definition.
    ///   Files from nested deployment definitions are excluded.
    ///
    /// - Deploy: Processes ALL files from ALL deployment definitions without filtering based on
    ///   the input path. Each file retains its respective deployment definition's variant tags.
    ///
    /// Why the difference?
    /// - Fetch is designed to work on a specific scope (one deployment definition at a time)
    ///   to avoid unintentionally fetching nested configurations. (only one input at the time)
    /// - Deploy needs to handle the entire project structure, so it processes all files and
    ///   respects each file's individual deployment definition settings. (multiple input at the time)
    /// </summary>
    [Test]
    public async Task FetchVsDeploy_MultiDdefBehavior_FetchFiltersDeployProcessesAll()
    {
        // Setup: Create a directory structure with root and nested deployment definitions
        // player/
        //   ├── player.ddef (tags: player, xbox)
        //   ├── asset.test
        //   ├── coord.test
        //   └── ios/
        //       ├── ios.ddef (tags: player, ios)
        //       └── coord.test

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

        // Test 1: Fetch should only process files from the root ddef
        var fetchInput = new FetchInput
        {
            Services = new[] { "test" },
            Path = "player/"
        };

        await FetchCommandHandler.FetchAsync(
            m_Host.Object,
            fetchInput,
            m_MockEnvironment.Object,
            m_Logger.Object,
            (StatusContext?)null,
            m_DdefService.Object,
            m_AnalyticsEventBuilder.Object,
            CancellationToken.None);

        // Verify: Fetch processes only 2 files (from root ddef), excluding nested ddef file
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
            Times.Once,
            "Fetch should filter to only files from the input path's deployment definition");

        // Reset for deploy test
        m_DdefService.Reset();
        m_DdefService
            .Setup(s => s.ResolveAuthoringFiles(
                It.IsAny<IReadOnlyList<string>>(),
                It.IsAny<IReadOnlyList<string>>()))
            .Returns(ddefResult);

        // Test 2: Deploy should process ALL files from ALL ddefs
        var deployInput = new DeployInput
        {
            Services = new[] { "test" }
        };

        await DeployCommandHandler.DeployAsync(
            m_Host.Object,
            deployInput,
            m_MockEnvironment.Object,
            m_Logger.Object,
            (StatusContext?)null,
            m_DdefService.Object,
            m_AnalyticsEventBuilder.Object,
            CancellationToken.None);

        // Verify: Deploy processes ALL 3 files, each with their respective variant tags
        m_DeploymentService.Verify(
            s => s.Deploy(
                It.IsAny<DeployInput>(),
                It.Is<IReadOnlyList<AuthoringFile>>(files =>
                    files.Count == 3 &&
                    files.Any(f => f.Path == "player/asset.test" &&
                        f.VariantTags.SequenceEqual(new[] { "player", "xbox" })) &&
                    files.Any(f => f.Path == "player/coord.test" &&
                        f.VariantTags.SequenceEqual(new[] { "player", "xbox" })) &&
                    files.Any(f => f.Path == "player/ios/coord.test" &&
                        f.VariantTags.SequenceEqual(new[] { "player", "ios" }))),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<StatusContext?>(),
                It.IsAny<CancellationToken>()),
            Times.Once,
            "Deploy should process all files from all deployment definitions with their respective tags");
    }
}
