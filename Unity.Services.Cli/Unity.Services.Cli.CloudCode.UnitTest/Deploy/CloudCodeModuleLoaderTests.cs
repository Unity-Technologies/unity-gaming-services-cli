using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using Unity.Services.Cli.Authoring.Model;
using Unity.Services.Cli.Authoring.Service;
using Unity.Services.Cli.CloudCode.Deploy;
using Unity.Services.CloudCode.Authoring.Editor.Core.Model;

namespace Unity.Services.Cli.CloudCode.UnitTest.Deploy;

[TestFixture]
public class CloudCodeModuleLoaderTests
{

    readonly Mock<ICliDeploymentOutputHandler> m_MockCliDeploymentOutputHandler = new();

    static readonly IReadOnlyCollection<DeployContent> k_DeployedContents = new[]
    {
        new DeployContent("module.zip", "Cloud Code Modules", "path", 100, "Published"),
    };

    static readonly IReadOnlyCollection<DeployContent> k_FailedContents = new[]
    {
        new DeployContent("invalid1.zip", "Cloud Code Modules", "path", 0, "Failed to Load"),
        new DeployContent("invalid2.zip", "Cloud Code Modules", "path", 0, "Failed to Load"),
    };

    List<DeployContent> m_Contents = k_DeployedContents.Concat(k_FailedContents).ToList();

    static readonly List<string> k_ValidZipPaths = new()
    {
        "new/path/to/test_a.zip",
        "new/path/to/test_b.zip"
    };

    [SetUp]
    public void SetUp()
    {
        m_MockCliDeploymentOutputHandler.Reset();
        m_MockCliDeploymentOutputHandler.SetupGet(c => c.Contents).Returns(m_Contents);
    }

    [Test]
    public async Task LoadPrecompiledModulesAsync_Deploys()
    {
        IScript test_a_module = new Unity.Services.Cli.CloudCode.Deploy.CloudCodeModule(
            new ScriptName("test_a.zip"),
            Language.JS,
            "new/path/to/test_a.zip");

        IScript test_b_module = new Unity.Services.Cli.CloudCode.Deploy.CloudCodeModule(
            new ScriptName("test_b.zip"),
            Language.JS,
            "new/path/to/test_b.zip");
        var expected = new List<IScript>
        {
            test_a_module,
            test_b_module
        };

        var cloudCodeModulesLoader = new CloudCodeModulesLoader();

        var actual = await cloudCodeModulesLoader.LoadPrecompiledModulesAsync(
            k_ValidZipPaths,
            "Cloud Code Modules",
            ".zip",
            m_Contents);

        CompareScripts(expected, actual);
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
