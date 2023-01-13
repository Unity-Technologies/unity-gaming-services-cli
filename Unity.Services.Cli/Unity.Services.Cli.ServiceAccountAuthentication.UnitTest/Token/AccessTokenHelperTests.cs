using NUnit.Framework;
using Unity.Services.Cli.ServiceAccountAuthentication.Token;

namespace Unity.Services.Cli.Authentication.UnitTest;

[TestFixture]
class AccessTokenHelperTests
{
    [Test]
    public void ToHeaderValueFormatsAsExpected()
    {
        const string token = "test token";
        const string expected = $"Basic {token}";

        var headerValue = AccessTokenHelper.ToHeaderValue(token);

        Assert.AreEqual(expected, headerValue);
    }

    [Test]
    public void SetAccessTokenHeaderCreatesEntryIfNoneExist()
    {
        const string token = "test token";
        var expectedTokenHeader = AccessTokenHelper.ToHeaderValue(token);
        var map = new Dictionary<string, string>();

        map.SetAccessTokenHeader(token);

        Assert.IsTrue(map.TryGetValue(AccessTokenHelper.HeaderKey, out var tokenHeader));
        Assert.AreEqual(expectedTokenHeader, tokenHeader);
    }

    [Test]
    public void SetAccessTokenHeaderOverridesEntryIfOneExists()
    {
        const string oldToken = "old token";
        const string newToken = "test token";
        var expectedTokenHeader = AccessTokenHelper.ToHeaderValue(newToken);
        var map = new Dictionary<string, string>
        {
            [AccessTokenHelper.HeaderKey] = oldToken,
        };

        map.SetAccessTokenHeader(newToken);

        Assert.IsTrue(map.TryGetValue(AccessTokenHelper.HeaderKey, out var tokenHeader));
        Assert.AreEqual(expectedTokenHeader, tokenHeader);
    }
}
