using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using Unity.Services.Cli.CloudCode.IO;
using Unity.Services.Cli.CloudCode.Solution;

namespace Unity.Services.Cli.CloudCode.UnitTest.Solution;

class FileContentRetrieverTests
{
    Mock<IAssemblyLoader> m_MockAssemblyLoader;
    FileContentRetriever m_FileContentRetriever;

    public FileContentRetrieverTests()
    {
        m_MockAssemblyLoader = new Mock<IAssemblyLoader>();
        m_MockAssemblyLoader
            .Setup(al => al.Load(It.IsAny<string>()))
            .Returns(Assembly.Load(FileContentRetriever.AssemblyString));
        m_FileContentRetriever = new FileContentRetriever(m_MockAssemblyLoader.Object);
    }

    class TestAssembly : Assembly { }

    [Test]
    public void GetFileContent_InvalidAssembly()
    {
        const string invalidAssemblyName = "invalid.assembly.name";
        m_MockAssemblyLoader
            .Setup(al => al.Load(invalidAssemblyName))
            .Returns(new TestAssembly());
        Assert.ThrowsAsync<FileLoadException>(() => m_FileContentRetriever.GetFileContent("some.invalid.path"));
    }

    [Test]
    public async Task GetFileContent_CanLoadSolution()
    {
        var templateInfo = new TemplateInfo();
        var fileContent = await m_FileContentRetriever.GetFileContent(templateInfo.PathSolution);
        Assert.IsFalse(string.IsNullOrEmpty(fileContent));
    }

    [Test]
    public async Task GetFileContent_CanLoadProject()
    {
        var templateInfo = new TemplateInfo();
        var fileContent = await m_FileContentRetriever.GetFileContent(templateInfo.PathConfig);
        Assert.IsFalse(string.IsNullOrEmpty(fileContent));
    }

    [Test]
    public async Task GetFileContent_CanLoadExample()
    {
        var templateInfo = new TemplateInfo();
        var fileContent = await m_FileContentRetriever.GetFileContent(templateInfo.PathExampleClass);
        Assert.IsFalse(string.IsNullOrEmpty(fileContent));
    }

    [Test]
    public async Task GetFileContent_CanLoadConfig()
    {
        var templateInfo = new TemplateInfo();
        var fileContent = await m_FileContentRetriever.GetFileContent(templateInfo.PathConfig);
        Assert.IsFalse(string.IsNullOrEmpty(fileContent));
    }

    [Test]
    public async Task GetFileContent_CanLoadConfigUser()
    {
        var templateInfo = new TemplateInfo();
        var fileContent = await m_FileContentRetriever.GetFileContent(templateInfo.PathConfigUser);
        Assert.IsFalse(string.IsNullOrEmpty(fileContent));
    }
}
