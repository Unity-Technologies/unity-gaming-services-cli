using NUnit.Framework;
using Unity.Services.Cli.CloudCode.IO;
using Unity.Services.Cli.CloudCode.Solution;

namespace Unity.Services.Cli.CloudCode.UnitTest.IO;

class AssemblyLoaderTests
{
    AssemblyLoader m_AssemblyLoader;

    public AssemblyLoaderTests()
    {
        m_AssemblyLoader = new AssemblyLoader();
    }

    [Test]
    public void LoadsCorrectAssembly()
    {
        var assembly = m_AssemblyLoader.Load(FileContentRetriever.AssemblyString);
        Assert.IsTrue(assembly.FullName?.StartsWith(FileContentRetriever.AssemblyString));
    }
}
