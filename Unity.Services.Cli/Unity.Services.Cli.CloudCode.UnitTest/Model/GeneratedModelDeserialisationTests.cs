using NUnit.Framework;
using Unity.Services.Gateway.CloudCodeApiV1.Generated.Model;

namespace Unity.Services.Cli.CloudCode.UnitTest.Model;

/// <summary>
/// The spec marks several fields both `required` and `nullable`, which is correct — the backend
/// always emits the key and its value may be null. The csharp-netcore generator turns `required`
/// into a null guard in the parameterised constructor, which makes hand-construction in tests
/// awkward, but it also emits a `[JsonConstructor] protected` parameterless constructor that
/// deserialisation uses instead. These tests pin that: a live response carrying nulls must
/// deserialise rather than throw.
/// </summary>
[TestFixture]
public class GeneratedModelDeserialisationTests
{
    const string k_ModuleWithNullVersionFields = @"{
      ""name"": ""CancellationTest"",
      ""language"": ""CS"",
      ""dateCreated"": ""2026-09-01T09:14:02Z"",
      ""dateModified"": ""2026-09-08T11:47:37Z"",
      ""versions"": [
        {
          ""version"": 1788855704118226,
          ""isLive"": false,
          ""signedDownloadURL"": ""https://example.com/x"",
          ""dateCreated"": null,
          ""endpoints"": null
        }
      ]
    }";

    // endpoints is null whenever the per-generation spec pass fails, which the backend treats as
    // normal and does not fail getModule over.
    [Test]
    public void ModuleVersion_WithNullEndpointsAndDate_Deserialises()
    {
        var response =
            Newtonsoft.Json.JsonConvert.DeserializeObject<GetModuleResponse>(k_ModuleWithNullVersionFields);

        Assert.That(response?.Versions, Is.Not.Null);
        Assert.That(response!.Versions![0].Endpoints, Is.Null);
        Assert.That(response.Versions[0].DateCreated, Is.Null);
        Assert.That(response.Versions[0]._Version, Is.EqualTo(1788855704118226L));
    }

    const string k_ScriptWithWorkingCopy = @"{
      ""name"": ""mtt15704-vdel"",
      ""type"": ""API"",
      ""language"": ""JS"",
      ""activeScript"": null,
      ""params"": [],
      ""versions"": [
        { ""code"": ""x"", ""params"": [], ""version"": null, ""isDraft"": true, ""dateUpdated"": ""2026-09-09T11:03:43Z"" }
      ]
    }";

    // The working copy's version is null, which is why it cannot be hand-constructed either.
    [Test]
    public void ScriptVersion_WithNullVersion_Deserialises()
    {
        var response =
            Newtonsoft.Json.JsonConvert.DeserializeObject<GetScriptResponse>(k_ScriptWithWorkingCopy);

        Assert.That(response?.Versions, Is.Not.Null);
        Assert.That(response!.Versions[0]._Version, Is.Null);
        Assert.That(response.Versions[0].IsDraft, Is.True);
    }

    const string k_ScriptListWithNeverPublished = @"{
      ""results"": [
        { ""name"": ""example-string"", ""type"": ""API"", ""language"": ""JS"",
          ""published"": false, ""lastPublishedDate"": null, ""lastPublishedVersion"": null }
      ],
      ""links"": { ""next"": """" }
    }";

    // A script that has never been published reports a null lastPublishedDate, which the CLI
    // renders as "Date Created: Never".
    [Test]
    public void ScriptListItem_WithNullLastPublishedDate_Deserialises()
    {
        var response =
            Newtonsoft.Json.JsonConvert.DeserializeObject<ListScriptsResponse>(k_ScriptListWithNeverPublished);

        Assert.That(response?.Results, Is.Not.Null);
        Assert.That(response!.Results[0].LastPublishedDate, Is.Null);
    }
}
