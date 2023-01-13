using NUnit.Framework;
using Unity.Services.Cli.Common.Networking;

namespace Unity.Services.Cli.Common.UnitTest;

[TestFixture]
class RequestHeaderHelperTests
{
    [Test]
    public void SetAccessTokenHeaderCreatesEntryIfNoneExist()
    {
        var map = new Dictionary<string, string>();
        map.SetXClientIdHeader();

        Assert.IsTrue(map.TryGetValue(RequestHeaderHelper.XClientIdHeaderKey, out var requestHeader));
        Assert.AreEqual(RequestHeaderHelper.XClientIdHeaderValue, requestHeader);
    }
}
