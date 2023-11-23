using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using Unity.Services.Cli.CloudCode.Deploy;
using Unity.Services.CloudCode.Authoring.Editor.Core.Deployment.ModuleGeneration;
using Unity.Services.CloudCode.Authoring.Editor.Core.Model;
using CCModule = Unity.Services.Cli.CloudCode.Deploy.CloudCodeModule;


namespace Unity.Services.Cli.CloudCode.UnitTest.Deploy;

[TestFixture]
public class CloudCodeModuleLoaderTests
{
    readonly Mock<IModuleBuilder> m_MockModuleBuilder = new();

    static readonly List<string> k_CcmPaths = new()
    {
        "new/path/to/test_a.ccm",
        "new/path/to/test_b.ccm"
    };

    static readonly List<string> k_SlnPaths = new()
    {
        "new/path/to/sln/test_a.sln",
        "new/path/to/sln/test_b.sln"
    };

    IScript m_TestAModule = new CCModule(
        new ScriptName("test_a.ccm"),
        Language.JS,
        k_CcmPaths[0]);

    IScript m_TestBModule = new CCModule(
        new ScriptName("test_b.ccm"),
        Language.JS,
        k_CcmPaths[1]);

    [SetUp]
    public void SetUp()
    {
        m_MockModuleBuilder.Reset();
    }

    [Test]
    public async Task LoadPrecompiledModules()
    {
        var cloudCodeModulesLoader = new CloudCodeModulesLoader(m_MockModuleBuilder.Object);

        var (generatedModules, failedModules) = await cloudCodeModulesLoader.LoadModulesAsync(
            k_CcmPaths,
            new List<string>(),
            CancellationToken.None);

        var expected = new List<IScript>
        {
            m_TestAModule,
            m_TestBModule
        };
        CompareScripts(expected, generatedModules);
    }

    [Test]
    public async Task LoadFailedModules()
    {
        m_MockModuleBuilder
            .Setup(x => x.CreateCloudCodeModuleFromSolution(
                It.IsAny<IModuleItem>(), It.IsAny<CancellationToken>()))
            .Throws(new Exception("failed"));

        var cloudCodeModulesLoader = new CloudCodeModulesLoader(m_MockModuleBuilder.Object);

        var (_, failedModules) =
            await cloudCodeModulesLoader.LoadModulesAsync(k_CcmPaths, k_SlnPaths, CancellationToken.None);

        Assert.AreEqual(k_SlnPaths.Count, failedModules.Count);
        for (int i = 0; i < k_SlnPaths.Count; i++)
        {
            Assert.AreEqual(k_SlnPaths[i], ((CCModule)failedModules[i]).SolutionPath);
        }
    }

    [Test]
    public async Task LoadPrecompiledAndSolutions()
    {
        m_MockModuleBuilder.Setup(
                x => x.CreateCloudCodeModuleFromSolution(
                    It.IsAny<IModuleItem>(),
                    It.IsAny<CancellationToken>()))
            .Callback<IModuleItem, CancellationToken>((m, _) => { m.CcmPath = m_TestBModule.Path; });

        var cloudCodeModulesLoader = new CloudCodeModulesLoader(m_MockModuleBuilder.Object);

        var (generatedModules, failedModules) = await cloudCodeModulesLoader.LoadModulesAsync(
            new List<string>() { k_CcmPaths[0] },
            new List<string>() { k_SlnPaths[1] },
            CancellationToken.None);

        var expected = new List<IScript>
        {
            m_TestAModule,
            m_TestBModule
        };
        Assert.AreEqual(expected.Count, generatedModules.Count);
        for (int i = 0; i < expected.Count; i++)
        {
            Assert.AreEqual(expected[i].Path, generatedModules[i].Path);
        }
    }

    static void CompareScripts(List<IScript> expected, List<IScript> actual)
    {
        Assert.AreEqual(expected.Count, actual.Count);

        for (int i = 0; i < expected.Count; i++)
        {
            Assert.AreEqual(expected[i].Name, actual[i].Name);
            Assert.AreEqual(expected[i].Language, actual[i].Language);
            Assert.AreEqual(expected[i].Path, actual[i].Path);
        }
    }
}
