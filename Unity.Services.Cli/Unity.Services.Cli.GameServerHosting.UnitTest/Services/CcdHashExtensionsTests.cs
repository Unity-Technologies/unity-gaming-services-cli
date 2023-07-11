using System.Text;
using System.Text.RegularExpressions;
using Unity.Services.Multiplay.Authoring.Core.CloudContent;

namespace Unity.Services.Cli.GameServerHosting.UnitTest.Services;

public class CcdHashExtensionsTests
{
    [Test]
    public void Hash_ReturnsLowerHex()
    {
        var data = new MemoryStream(Encoding.UTF8.GetBytes("hello world!"));
        var hash = data.CcdHash();

        Assert.IsTrue(Regex.IsMatch(hash, "^[a-fA-F0-9]+$"));
    }
}
