using Newtonsoft.Json.Linq;
using NUnit.Framework;
using Unity.Services.Cli.Common.Exceptions;
using Unity.Services.Cli.Player.IdProvider.Models;
using Unity.Services.Cli.Player.IdProvider.Registry;

namespace Unity.Services.Cli.Player.UnitTest.IdProvider;

public class IdProviderTypesTests
{
    [TestCase("steampowered.com", "steampowered.com")]
    [TestCase("steam", "steampowered.com")]
    [TestCase("Steam", "steampowered.com")]
    [TestCase("steampowered", "steampowered.com")]
    [TestCase("apple", "apple.com")]
    [TestCase("Sign-in with Apple", "apple.com")]
    [TestCase("SigninwithApple", "apple.com")]
    [TestCase("applegamecenter", "apple-game-center")]
    [TestCase("google", "google.com")]
    [TestCase("GooglePlayGames", "google-play-games")]
    [TestCase("  custom  ", "custom")]
    [TestCase("unity", "unity")]
    public void Resolve_AcceptsTypesNamesAndAliases(string provider, string expectedType)
    {
        Assert.That(IdProviderTypes.Resolve(provider).Type, Is.EqualTo(expectedType));
    }

    [TestCase("oidc-mygame")]
    [TestCase("oidc-a.b_c-1")]
    public void Resolve_KeepsTheOpenIdConnectName(string provider)
    {
        var resolved = IdProviderTypes.Resolve(provider);

        Assert.That(resolved.Type, Is.EqualTo(provider));
        Assert.That(resolved.IsOpenIdConnect, Is.True);
    }

    [TestCase("oidc-MyGame")]
    [TestCase("oidc-")]
    [TestCase("oidc-sixteen-chars-xy")]
    [TestCase("oidc")]
    [TestCase("OpenID Connect")]
    [TestCase("Xbox Live")]
    [TestCase("-leading-dash")]
    [TestCase("")]
    [TestCase("   ")]
    [TestCase(null)]
    public void Resolve_RefusesWhatTheApiCannotAddress(string? provider)
    {
        var exception = Assert.Throws<CliException>(() => IdProviderTypes.Resolve(provider));

        Assert.That(exception!.ExitCode, Is.EqualTo(ExitCode.HandledError));
    }

    [TestCase("xbox")]
    [TestCase("not-a-provider")]
    [TestCase("example.com")]
    public void Resolve_PassesATypeThisCliDoesNotKnowThrough(string provider)
    {
        var resolved = IdProviderTypes.Resolve(provider);

        Assert.That(resolved.Type, Is.EqualTo(provider));
        Assert.That(resolved.IsKnown, Is.False);
        Assert.That(resolved.Definition.CanBeCreated, Is.True);
    }

    [Test]
    public void Find_MapsAnyOpenIdConnectTypeToItsDefinition()
    {
        Assert.That(IdProviderTypes.Find("oidc-mygame")?.DisplayName, Is.EqualTo("OpenID Connect"));
        Assert.That(IdProviderTypes.Find("unknown.example"), Is.Null);
    }

    [Test]
    public void OnlyUsernamePasswordCannotBeDeleted()
    {
        var undeletable = IdProviderTypes.All.Where(definition => !definition.CanBeDeleted).Select(d => d.Type);

        Assert.That(undeletable, Is.EqualTo(new[] { "usernamepassword" }));
    }

    [Test]
    public void OnlyUnityPlayerAccountsCannotBeCreated()
    {
        var uncreatable = IdProviderTypes.All.Where(definition => !definition.CanBeCreated).Select(d => d.Type);

        Assert.That(uncreatable, Is.EqualTo(new[] { "unity" }));
    }

    [Test]
    public void TypeResult_PrintsAsJsonInTextOutput()
    {
        var text = IdProviderTypeResult.From(IdProviderTypes.Find("steampowered.com")!).ToString();

        Assert.That(JObject.Parse(text!)["type"]!.Value<string>(), Is.EqualTo("steampowered.com"));
    }
}
