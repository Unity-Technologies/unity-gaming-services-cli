using System.IO.Abstractions;
using Moq;
using Unity.Services.Cli.GameServerHosting.Services;

namespace Unity.Services.Cli.GameServerHosting.UnitTest.Services;

[TestFixture]
[TestOf(typeof(GcsCredentialParser))]
public class GcsCredentialParserTest
{
    const string k_ValidFilePath = "validFilePath";


    [TestCase(
        k_ValidFilePath,
        "{\"client_email\":\"valid access id\",\"private_key\":\"valid private key\"}",
        "valid access id",
        "valid private key",
        TestName = "happy path"
    )]
    [TestCase(
        "invalid path",
        "",
        "valid access id",
        "valid private key",
        true,
        "File not found",
        TestName = "file doesn't exist"
    )]
    [TestCase(
        k_ValidFilePath,
        "qwdqw qwdqw",
        "valid access id",
        "valid private key",
        true,
        "Invalid JSON format",
        TestName = "invalid json"
    )]
    [TestCase(
        k_ValidFilePath,
        "{}",
        "valid access id",
        "valid private key",
        true,
        "`private_key` or `client_email` are empty",
        TestName = "empty credentials"
    )]
    public void Parse(
        string path,
        string content,
        string expectedAccessId,
        string expectedPrivateKey,
        bool expectedException = false,
        string expectedExceptionMessage = "")
    {
        var mockFileSystem = new Mock<IFile>();

        mockFileSystem.Setup(m => m.Exists(It.Is<string>(s => s == k_ValidFilePath))).Returns(true);
        mockFileSystem.Setup(m => m.ReadAllText(It.Is<string>(s => s == k_ValidFilePath))).Returns(content);

        var parser = new GcsCredentialParser(mockFileSystem.Object);

        try
        {
            var credentials = parser.Parse(path);

            Assert.Multiple(
                () =>
                {
                    Assert.That(credentials, Is.Not.Null);
                    Assert.That(credentials.ClientEmail, Is.EqualTo(expectedAccessId));
                    Assert.That(credentials.PrivateKey, Is.EqualTo(expectedPrivateKey));
                });
        }
        catch (Exception e)
        {
            if (!expectedException)
            {
                Assert.Fail($"Unexpected exception: {e}");
            }

            Assert.That(e.Message, Does.Contain(expectedExceptionMessage));
            return;
        }

        if (expectedException)
        {
            Assert.Fail($"Expected exception: {expectedExceptionMessage}");
        }
    }
}
