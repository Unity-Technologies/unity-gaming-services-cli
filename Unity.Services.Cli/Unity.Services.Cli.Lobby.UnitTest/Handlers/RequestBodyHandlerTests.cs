using NUnit.Framework;
using Unity.Services.Cli.Lobby.Handlers;
using Unity.Services.Cli.Common.Exceptions;

namespace Unity.Services.Cli.Lobby.UnitTest.Handlers;

[TestFixture]
class RequestBodyHandlerTests
{
    [Test]
    public void RequestBodyHandler_ReadsFile()
    {
        const string path = "test.txt";
        const string content = "foobar";
        File.WriteAllText(path, content);

        var requestBody = RequestBodyHandler.GetRequestBodyFromFileOrInput(path);
        File.Delete(path);
        Assert.AreEqual(content, requestBody);
    }

    [Test]
    public void RequestBodyHandler_ReadsInput()
    {
        const string inputString = "{}";
        var requestBody = RequestBodyHandler.GetRequestBodyFromFileOrInput(inputString);
        Assert.AreEqual(inputString, requestBody);
    }

    [Test]
    public void RequestBodyHandler_ReturnsEmptyStringIfOptionalAndNull()
    {
        var requestBody = RequestBodyHandler.GetRequestBodyFromFileOrInput(string.Empty);
        Assert.AreEqual(string.Empty, requestBody);
    }

    [Test]
    public void RequestBodyHandler_ThrowsIfRequiredAndNullOrEmpty()
    {
        foreach (var inputString in new List<string?>() { null, "" })
        {
            Assert.Throws<CliException>(() => RequestBodyHandler.GetRequestBodyFromFileOrInput(inputString, isRequired: true));
        }
    }
}
