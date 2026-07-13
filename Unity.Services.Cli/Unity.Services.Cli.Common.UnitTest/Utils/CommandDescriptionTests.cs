using NUnit.Framework;

namespace Unity.Services.Cli.Common.UnitTest.Utils;

[TestFixture]
class CommandDescriptionTests
{
    [Test]
    public void BodyOnly_ReturnsBodyUnchanged()
    {
        var result = new CommandDescription("Manage badges for a release.").Build();
        Assert.That(result, Is.EqualTo("Manage badges for a release."));
    }

    [Test]
    public void WithReturn_AppendsReturnsSection()
    {
        var result = new CommandDescription("List leaderboards.")
            .WithReturn("JSON array of leaderboard summaries with id and name.")
            .Build();
        Assert.That(result, Is.EqualTo(
            "List leaderboards. Returns: JSON array of leaderboard summaries with id and name."));
    }

    [Test]
    public void WithDocs_AppendsServiceDocsSection()
    {
        var result = new CommandDescription("Manage In-App Purchasing catalog items.")
            .WithDocs("https://docs.unity.com/ugs/manual/iap/manual")
            .Build();
        Assert.That(result, Is.EqualTo(
            "Manage In-App Purchasing catalog items. Service docs: https://docs.unity.com/ugs/manual/iap/manual."));
    }

    [Test]
    public void WithDocsAndApi_MatchesRootModuleFormat()
    {
        var result = new CommandDescription("Manage Leaderboards.")
            .WithDocs("https://docs.unity.com/ugs/manual/leaderboards/manual")
            .WithAdminApi("https://services.docs.unity.com/leaderboards-admin/v1/")
            .Build();
        Assert.That(result, Is.EqualTo(
            "Manage Leaderboards."
            + " Service docs: https://docs.unity.com/ugs/manual/leaderboards/manual."
            + " Admin API docs: https://services.docs.unity.com/leaderboards-admin/v1/"));
    }

    [Test]
    public void WithDocsAndAdminApiAndClientApi_MatchesPlayerModuleFormat()
    {
        var result = new CommandDescription("Manage your player accounts in Unity Authentication Service.")
            .WithDocs("https://docs.unity.com/ugs/manual/authentication/manual")
            .WithAdminApi("https://services.docs.unity.com/player-auth-admin/v1/")
            .WithClientApi("https://services.docs.unity.com/player-auth/v1/")
            .Build();
        Assert.That(result, Is.EqualTo(
            "Manage your player accounts in Unity Authentication Service."
            + " Service docs: https://docs.unity.com/ugs/manual/authentication/manual."
            + " Admin API docs: https://services.docs.unity.com/player-auth-admin/v1/"
            + " Client API docs: https://services.docs.unity.com/player-auth/v1/"));
    }

    [Test]
    public void WithDocsAndClientApiOnly_MatchesLobbyModuleFormat()
    {
        var result = new CommandDescription("Interact with the Lobby service.")
            .WithDocs("https://docs.unity.com/ugs/manual/lobby/manual")
            .WithClientApi("https://services.docs.unity.com/lobby/v1/")
            .Build();
        Assert.That(result, Is.EqualTo(
            "Interact with the Lobby service."
            + " Service docs: https://docs.unity.com/ugs/manual/lobby/manual."
            + " Client API docs: https://services.docs.unity.com/lobby/v1/"));
    }

    [Test]
    public void BodyWithNewlines_PreservesNewlines()
    {
        var result = new CommandDescription(
                "Sync entries from local directory for current bucket.\n"
                + "Automatically creates, updates, and deletes entries\n"
                + "within the bucket to match the files in the local directory.")
            .WithReturn("operation summary.")
            .Build();
        Assert.That(result, Does.Contain("\n"));
        Assert.That(result, Does.EndWith(" Returns: operation summary."));
    }

    [Test]
    public void SectionOrdering_DocsBeforeApiBeforeReturns()
    {
        var result = new CommandDescription("Test.")
            .WithReturn("some result.")
            .WithDocs("https://docs.example.com")
            .WithAdminApi("https://api.example.com")
            .Build();

        var docsIndex = result.IndexOf("Service docs:", StringComparison.Ordinal);
        var apiIndex = result.IndexOf("API docs:", StringComparison.Ordinal);
        var returnsIndex = result.IndexOf("Returns:", StringComparison.Ordinal);

        Assert.That(docsIndex, Is.LessThan(apiIndex));
        Assert.That(apiIndex, Is.LessThan(returnsIndex));
    }
}
