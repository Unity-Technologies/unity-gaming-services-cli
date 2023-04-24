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
using Unity.Services.Cli.TestUtils;

namespace Unity.Services.Cli.Authoring.UnitTest.Handlers;

public class NewFileHandlerTests
{
    Mock<IFile>? m_MockFile;
    Mock<IFileTemplate>? m_MockTemplate;
    Mock<ILogger>? m_MockLogger;
    CancellationToken m_CancellationToken;
    const string k_FileExtension = ".test";
    const string k_FileName = "test";
    const string k_ExistingFileName = "test_exists";

    [SetUp]
    public void SetUp()
    {
        m_MockFile = new Mock<IFile>();
        m_MockTemplate = new Mock<IFileTemplate>();
        m_MockTemplate.SetupGet(template => template.Extension).Returns(k_FileExtension);
        m_MockLogger = new Mock<ILogger>();
        m_CancellationToken = CancellationToken.None;

        m_MockFile!.Setup(e => e.Exists(k_ExistingFileName + k_FileExtension)).Returns(true);
    }

    [Test]
    public async Task NewFile_WithInvalidExtension_ReplacedWithValid()
    {
        var input = new NewFileInput
        {
            File = "test.txt"
        };
        await NewFileHandler.NewFileAsync(input, m_MockFile!.Object, m_MockTemplate!.Object, m_MockLogger!.Object, m_CancellationToken);
        Assert.That(Path.GetExtension(input.File), Is.EqualTo(k_FileExtension));
    }

    [Test]
    public async Task NewFile_CallsWriteAllText()
    {
        await NewFileHandler.NewFileAsync(new NewFileInput { File = k_FileName }, m_MockFile!.Object, m_MockTemplate!.Object, m_MockLogger!.Object, m_CancellationToken);

        m_MockFile.Verify(f => f.WriteAllTextAsync(It.IsAny<string>(), It.IsAny<string>(), CancellationToken.None), Times.Once);
    }

    [Test]
    public async Task NewFile_ErrorsOutIfFileExists()
    {
        await NewFileHandler.NewFileAsync(new NewFileInput { File = k_ExistingFileName }, m_MockFile!.Object, m_MockTemplate!.Object, m_MockLogger!.Object, m_CancellationToken);

        m_MockFile.Verify(f => f.WriteAllTextAsync(It.IsAny<string>(), It.IsAny<string>(), CancellationToken.None), Times.Never);
        TestsHelper.VerifyLoggerWasCalled(m_MockLogger, LogLevel.Error);
    }
}
