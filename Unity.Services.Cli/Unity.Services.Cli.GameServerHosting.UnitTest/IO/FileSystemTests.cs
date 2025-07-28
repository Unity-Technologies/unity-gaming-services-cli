using Moq;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Unity.Services.Cli.GameServerHosting.IO;

namespace Unity.Services.Cli.GameServerHosting.UnitTest.IO;
public class FileSystemTests
{
    [Test]
    public async Task WriteAllText_ShouldWriteCorrectContents()
    {
        var fileSystemMock = new Mock<IFileSystem>();
        var path = "test.txt";
        var contents = "Hello, World!";
        var cancellationToken = new CancellationToken();

        await fileSystemMock.Object.WriteAllText(path, contents, cancellationToken);

        fileSystemMock.Verify(fs => fs.WriteAllText(path, contents, cancellationToken), Times.Once);
    }

    [Test]
    public async Task Delete_ShouldCallDeleteOnFile()
    {
        var fileSystemMock = new Mock<IFileSystem>();
        var path = "test.txt";
        var cancellationToken = new CancellationToken();

        await fileSystemMock.Object.Delete(path, cancellationToken);

        fileSystemMock.Verify(fs => fs.Delete(path, cancellationToken), Times.Once);
    }
}
