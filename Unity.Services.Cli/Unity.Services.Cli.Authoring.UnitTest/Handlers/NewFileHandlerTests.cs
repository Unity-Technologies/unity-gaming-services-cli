using System.IO;
using System.IO.Abstractions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using Unity.Services.Cli.Authoring.Handlers;
using Unity.Services.Cli.Authoring.Input;
using Unity.Services.Cli.Authoring.Templates;

namespace Unity.Services.Cli.Authoring.UnitTest.Handlers;

public class NewFileHandlerTests
{
    Mock<IFile>? m_MockFile;
    Mock<IFileTemplate>? m_MockTemplate;
    Mock<ILogger>? m_MockLogger;
    CancellationToken m_CancellationToken;

    [SetUp]
    public void SetUp()
    {
        m_MockFile = new Mock<IFile>();
        m_MockTemplate = new Mock<IFileTemplate>();
        m_MockTemplate.SetupGet(template => template.Extension).Returns(".test");
        m_MockLogger = new Mock<ILogger>();
        m_CancellationToken = CancellationToken.None;
    }

    [Test]
    public async Task NewFile_WithInvalidExtension_ReplacedWithValid()
    {
        var input = new NewFileInput
        {
            File = "test.txt"
        };
        await NewFileHandler.NewFileAsync(input, m_MockFile!.Object, m_MockTemplate!.Object, m_MockLogger!.Object, m_CancellationToken);
        Assert.That(Path.GetExtension(input.File), Is.EqualTo(".test"));
    }

    [Test]
    public async Task NewFile_CallsWriteAllText()
    {
        var file = "test";

        await NewFileHandler.NewFileAsync(new NewFileInput { File = file }, m_MockFile!.Object, m_MockTemplate!.Object, m_MockLogger!.Object, m_CancellationToken);

        m_MockFile.Verify(f => f.WriteAllTextAsync(It.IsAny<string>(), It.IsAny<string>(), CancellationToken.None), Times.Once);
    }
}
