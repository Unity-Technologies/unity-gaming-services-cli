using System;
using System.Collections.Generic;
using NUnit.Framework;
using Unity.Services.Cli.CloudCode.Model;
using Unity.Services.Gateway.CloudCodeApiV1.Generated.Model;

namespace Unity.Services.Cli.CloudCode.UnitTest.Model;

[TestFixture]
public class OutputModuleTests
{
    [Test]
    public void ConstructOutputModule_WithNullOptionalFields()
    {
        var dt = DateTime.UtcNow;
        var response = new GetModuleResponse(
            name: "MyModule",
            language: "JS",
            tags: null,
            signedDownloadURL: null,
            dateModified: dt,
            dateCreated: dt);
        var output = new GetModuleResponseOutput(response);
        var yaml = output.ToString();

        Assert.That(yaml, Does.Contain("name: MyModule"));
        Assert.That(yaml, Does.Contain("language: JS"));
        Assert.That(yaml, Does.Contain("dateModified:"));
        Assert.That(yaml, Does.Contain("dateCreated:"));
        Assert.That(yaml, Does.Not.Contain("signedDownloadURL:"));
        Assert.That(yaml, Does.Not.Contain("tags:"));
    }

    [Test]
    public void ConstructOutputModule_WithSignedDownloadUrl()
    {
        var dt = DateTime.UtcNow;
        var response = new GetModuleResponse(
            name: "MyModule",
            language: "JS",
            tags: null,
            signedDownloadURL: "https://example.com/download",
            dateModified: dt,
            dateCreated: dt);
        var output = new GetModuleResponseOutput(response);
        var yaml = output.ToString();

        Assert.That(yaml, Does.Contain("name: MyModule"));
        Assert.That(yaml, Does.Contain("language: JS"));
        Assert.That(yaml, Does.Contain("dateModified:"));
        Assert.That(yaml, Does.Contain("dateCreated:"));
        Assert.That(yaml, Does.Contain("signedDownloadURL: https://example.com/download"));
    }

    [Test]
    public void ConstructOutputModule_WithTags()
    {
        var dt = DateTime.UtcNow;
        var tags = new Dictionary<string,string>
        {
            {"team", "core"},
            {"env", "prod"}
        };
        var response = new GetModuleResponse(
            name: "MyModule",
            language: "JS",
            tags: tags,
            signedDownloadURL: "https://example.com/download",
            dateModified: dt,
            dateCreated: dt);
        var output = new GetModuleResponseOutput(response);
        var yaml = output.ToString();

        Assert.That(yaml, Does.Contain("name: MyModule"));
        Assert.That(yaml, Does.Contain("language: JS"));
        Assert.That(yaml, Does.Contain("dateModified:"));
        Assert.That(yaml, Does.Contain("dateCreated:"));
        Assert.That(yaml, Does.Contain("tags:"));
        Assert.That(yaml, Does.Contain("team: core"));
        Assert.That(yaml, Does.Contain("env: prod"));
        Assert.That(yaml, Does.Contain("signedDownloadURL: https://example.com/download"));
    }

    [Test]
    public void ConstructOutputModule_HasErrorFalse_NoErrorMessage()
    {
        var dt = DateTime.UtcNow;
        var response = new GetModuleResponse(
            name: "MyModule",
            language: "JS",
            tags: null,
            signedDownloadURL: null,
            dateModified: dt,
            dateCreated: dt,
            hasError: false,
            errorMessage: null);

        var output = new GetModuleResponseOutput(response);
        var yaml = output.ToString();

        Assert.That(yaml, Does.Contain("hasError: false"));
        Assert.That(yaml, Does.Not.Contain("errorMessage:"));
    }

    [Test]
    public void ConstructOutputModule_HasErrorTrue_IncludesErrorMessage()
    {
        var dt = DateTime.UtcNow;
        var response = new GetModuleResponse(
            name: "MyModule",
            language: "JS",
            tags: null,
            signedDownloadURL: null,
            dateModified: dt,
            dateCreated: dt,
            hasError: true,
            errorMessage: "module endpoint generation failed");

        var output = new GetModuleResponseOutput(response);
        var yaml = output.ToString();

        Assert.That(yaml, Does.Contain("hasError: true"));
        Assert.That(yaml, Does.Contain("errorMessage: module endpoint generation failed"));
    }

    [Test]
    public void ConstructOutputModule_HasEndpoints()
    {
        var dt = DateTime.UtcNow;
        var response = new GetModuleResponse(
            name: "MyModule",
            language: "JS",
            tags: null,
            signedDownloadURL: null,
            dateModified: dt,
            dateCreated: dt,
            hasError: false,
            endpoints: new Dictionary<string, ModuleEndpoints1>
            {
                {
                    "SayHello",
                    new ModuleEndpoints1(
                        parameters: new Dictionary<string, string>
                        {
                            { "name", "System.String" }
                        },
                        returnType: "System.String"
                    )
                }
            }
        );

        var output = new GetModuleResponseOutput(response);
        var yaml = output.ToString();

        Assert.That(yaml, Does.Contain("endpoints:"));
        Assert.That(yaml, Does.Contain("SayHello:"));
        Assert.That(yaml, Does.Contain("parameters:"));
        Assert.That(yaml, Does.Contain("name: System.String"));
        Assert.That(yaml, Does.Contain("returnType: System.String"));
    }

    const string k_ReleaseTagKey = "releases.cloud.unity.com/aaedbb7b-4047-46bf-b20d-0f16bf1a9a9b";

    // signedDownloadURL, dateCreated and endpoints are all required by the contract, so the
    // generated constructor rejects null for them even where a test does not care about the value.
    // Note dateCreated is rejected despite being typed DateTime?, because required is enforced
    // independently of nullability.
    static ModuleVersion Version(
        long version,
        bool isLive,
        Dictionary<string, string>? tags = null,
        DateTime? dateCreated = null,
        string signedDownloadURL = "https://example.com/signed",
        Dictionary<string, ModuleEndpoints1>? endpoints = null)
    {
        return new ModuleVersion(
            version: version,
            isLive: isLive,
            tags: tags,
            signedDownloadURL: signedDownloadURL,
            dateCreated: dateCreated ?? new DateTime(2026, 9, 1, 9, 14, 2, DateTimeKind.Utc),
            endpoints: endpoints ?? new Dictionary<string, ModuleEndpoints1>());
    }

    static GetModuleResponse ModuleWithVersions(params ModuleVersion[] versions)
    {
        var dt = DateTime.UtcNow;
        return new GetModuleResponse(
            name: "MyModule",
            language: "CS",
            tags: null,
            signedDownloadURL: null,
            dateModified: dt,
            dateCreated: dt,
            versions: new List<ModuleVersion>(versions));
    }

    [Test]
    public void ConstructOutputModule_VersionsOmittedUnlessRequested()
    {
        var response = ModuleWithVersions(
            Version(1788856057800102L, isLive: true));

        var output = new GetModuleResponseOutput(response);

        Assert.That(output.Versions, Is.Null);
        Assert.That(output.ToString(), Does.Not.Contain("versions:"));
    }

    [Test]
    public void ConstructOutputModule_WithVersionsRequested_RendersVersions()
    {
        var created = new DateTime(2026, 9, 8, 11, 47, 37, DateTimeKind.Utc);
        var response = ModuleWithVersions(
            Version(
                1788856057800102L,
                isLive: true,
                tags: new Dictionary<string, string> { { "tier", "a" } },
                dateCreated: created,
                endpoints: new Dictionary<string, ModuleEndpoints1>
                {
                    { "SayHello", new ModuleEndpoints1(returnType: "System.String") }
                }),
            Version(1788855912340871L, isLive: false));

        var yaml = new GetModuleResponseOutput(response, includeVersions: true).ToString();

        Assert.That(yaml, Does.Contain("versions:"));
        Assert.That(yaml, Does.Contain("version: 1788856057800102"));
        Assert.That(yaml, Does.Contain("isLive: true"));
        Assert.That(yaml, Does.Contain("dateCreated: 2026-09-08T11:47:37"));
        Assert.That(yaml, Does.Contain("tier: a"));
        Assert.That(yaml, Does.Contain("version: 1788855912340871"));
        Assert.That(yaml, Does.Contain("isLive: false"));

        // Deliberately omitted from a version row: the signed URL is long, time-limited and already
        // shown at module level, and endpoints are served in full by `modules get-spec --version`.
        Assert.That(yaml, Does.Not.Contain("https://example.com/signed"));
        Assert.That(yaml, Does.Not.Contain("SayHello"));
    }

    [Test]
    public void ConstructOutputModule_VersionTags_ExcludeReservedKeys()
    {
        var response = ModuleWithVersions(
            Version(
                1788856057800102L,
                isLive: false,
                tags: new Dictionary<string, string>
                {
                    { "tier", "a" },
                    { k_ReleaseTagKey, "true" }
                }));

        var output = new GetModuleResponseOutput(response, includeVersions: true);
        var yaml = output.ToString();

        Assert.That(output.Versions![0].Tags, Is.EqualTo(new Dictionary<string, string> { { "tier", "a" } }));
        Assert.That(yaml, Does.Contain("tier: a"));
        Assert.That(yaml, Does.Not.Contain("releases.cloud.unity.com"));
    }

    [Test]
    public void ConstructOutputModule_VersionWithOnlyReservedTags_OmitsTags()
    {
        var response = ModuleWithVersions(
            Version(
                1788856057800102L,
                isLive: false,
                tags: new Dictionary<string, string> { { k_ReleaseTagKey, "true" } }));

        var output = new GetModuleResponseOutput(response, includeVersions: true);

        Assert.That(output.Versions![0].Tags, Is.Null);
        Assert.That(output.ToString(), Does.Not.Contain("tags:"));
    }

    [Test]
    public void ConstructOutputModule_VersionsRequestedButFeatureOff_OmitsVersions()
    {
        var dt = DateTime.UtcNow;
        var response = new GetModuleResponse(
            name: "MyModule",
            language: "CS",
            dateModified: dt,
            dateCreated: dt,
            versions: null);

        var output = new GetModuleResponseOutput(response, includeVersions: true);

        Assert.That(output.Versions, Is.Null);
        Assert.That(output.ToString(), Does.Not.Contain("versions:"));
    }

    [Test]
    public void ConstructOutputModule_VersionsRequestedAndEmpty_RendersEmptyList()
    {
        var output = new GetModuleResponseOutput(ModuleWithVersions(), includeVersions: true);

        Assert.That(output.Versions, Is.Empty);
        Assert.That(output.ToString(), Does.Contain("versions: []"));
    }

    [Test]
    public void ConstructOutputModule_VersionsJsonShapeMatchesYaml()
    {
        var created = new DateTime(2026, 9, 8, 11, 47, 37, DateTimeKind.Utc);
        var response = ModuleWithVersions(
            Version(
                1788856057800102L,
                isLive: true,
                tags: new Dictionary<string, string>
                {
                    { "tier", "a" },
                    { k_ReleaseTagKey, "true" }
                },
                dateCreated: created));

        var json = Newtonsoft.Json.JsonConvert.SerializeObject(
            new GetModuleResponseOutput(response, includeVersions: true));

        Assert.That(json, Does.Contain("1788856057800102"));
        Assert.That(json, Does.Contain("\"IsLive\":true"));
        Assert.That(json, Does.Contain("2026-09-08T11:47:37"));
        Assert.That(json, Does.Contain("tier"));
        // The same omissions must hold in JSON as in YAML, or -j leaks what the default format hides.
        Assert.That(json, Does.Not.Contain("releases.cloud.unity.com"));
        Assert.That(json, Does.Not.Contain("https://example.com/signed"));
        Assert.That(json, Does.Contain("\"Versions\""));
    }

    // -j serialises the object directly rather than via ToString(), so the null has to be suppressed
    // explicitly. Without this the default JSON shape gains a "Versions": null key it never had, and
    // a consumer cannot tell "not requested" from "versions are disabled for this project".
    [Test]
    public void ConstructOutputModule_JsonOmitsVersionsWhenNotRequested()
    {
        var response = ModuleWithVersions(Version(1788856057800102L, isLive: true));

        var json = Newtonsoft.Json.JsonConvert.SerializeObject(new GetModuleResponseOutput(response));

        Assert.That(json, Does.Not.Contain("Versions"));
    }

    // The absent case, which the populated-shape test above cannot cover: a retained version with no
    // user tags and an unknown creation date must omit those keys in JSON exactly as YAML does.
    [Test]
    public void ConstructOutputModule_JsonOmitsAbsentVersionFields()
    {
        var version = Version(1788856057800102L, isLive: false);
        version.DateCreated = null;

        var output = new GetModuleResponseOutput(ModuleWithVersions(version), includeVersions: true);
        var json = Newtonsoft.Json.JsonConvert.SerializeObject(output);

        var row = (Newtonsoft.Json.Linq.JObject)
            Newtonsoft.Json.Linq.JObject.Parse(json)["Versions"]![0]!;

        Assert.That(row.ContainsKey("Version"), Is.True);
        Assert.That(row.ContainsKey("IsLive"), Is.True);
        Assert.That(row.ContainsKey("Tags"), Is.False, "Tags must be omitted, not null");
        Assert.That(row.ContainsKey("DateCreated"), Is.False, "DateCreated must be omitted, not null");

        // And the YAML projection agrees: neither key appears anywhere, since the module-level tags
        // are also absent in this fixture.
        var yaml = output.ToString();
        Assert.That(yaml, Does.Not.Contain("tags:"));
        Assert.That(yaml, Does.Contain("versions:"));
    }

    [Test]
    public void ConstructOutputModule_JsonOmitsVersionsWhenFeatureOff()
    {
        var dt = DateTime.UtcNow;
        var response = new GetModuleResponse(
            name: "MyModule",
            language: "CS",
            dateModified: dt,
            dateCreated: dt,
            versions: null);

        var json = Newtonsoft.Json.JsonConvert.SerializeObject(
            new GetModuleResponseOutput(response, includeVersions: true));

        Assert.That(json, Does.Not.Contain("Versions"));
    }
}

