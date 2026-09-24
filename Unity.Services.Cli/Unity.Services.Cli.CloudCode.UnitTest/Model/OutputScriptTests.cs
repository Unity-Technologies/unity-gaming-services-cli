using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using NUnit.Framework;
using Unity.Services.Cli.CloudCode.Model;
using Unity.Services.Gateway.CloudCodeApiV1.Generated.Model;

namespace Unity.Services.Cli.CloudCode.UnitTest.Model;

[TestFixture]
class OutputScriptTests
{
    GetScriptResponse m_GetScriptResponse = new(
        "",
        "API",
        "JS",
        new GetScriptResponseActiveScript("", 0, _params: new List<ScriptParameter>()),
        new List<GetScriptResponseVersionsInner>(),
        new List<ScriptParameter>());

    [SetUp]
    public void SetUp()
    {
        const string scriptName = "Test";
        const string scriptType = "API";
        const string language = "JS";
        const string code = "";
        const int version = 0;
        var dateTime = new DateTime();
        var parameters = new List<ScriptParameter>();
        var script = new GetScriptResponseActiveScript(code, version, datePublished: dateTime, _params: parameters);
        var scriptResponseVersions = new List<GetScriptResponseVersionsInner>();
        m_GetScriptResponse = new GetScriptResponse(scriptName, scriptType, language, script, scriptResponseVersions, parameters);
    }

    [Test]
    public void ConstructOutputScriptWithValidResponse()
    {
        var outputScript = new GetScriptResponseOutput(m_GetScriptResponse);
        Assert.AreEqual(m_GetScriptResponse.Language, outputScript.Language);
        Assert.AreEqual(m_GetScriptResponse.Name, outputScript.Name);
        Assert.AreEqual(m_GetScriptResponse.Type, outputScript.Type);
        Assert.AreEqual(m_GetScriptResponse.ActiveScript.Code, outputScript.ActiveScript.Code);
        Assert.AreEqual(m_GetScriptResponse.ActiveScript.Params, outputScript.ActiveScript.Params);
        Assert.AreEqual(m_GetScriptResponse.ActiveScript._Version, outputScript.ActiveScript.Version);
        Assert.AreEqual(m_GetScriptResponse.ActiveScript.DatePublished.ToString("s", CultureInfo.InvariantCulture), outputScript.ActiveScript.DatePublished);
        Assert.AreEqual(m_GetScriptResponse.Versions, outputScript.Versions);
    }

    [Test]
    public void ConstructOutputScriptWithNullActiveScript()
    {
        m_GetScriptResponse.ActiveScript = null;
        Assert.DoesNotThrow(() => _ = new GetScriptResponseOutput(m_GetScriptResponse));
    }

    [Test]
    public void ConstructOutputScriptWithAValidVersionOutput()
    {
        const int version = 3;
        const bool isDraft = true;
        var dateUpdated = DateTime.Now;
        var dateCreated = DateTime.Today;
        m_GetScriptResponse.Versions.Add(
            new GetScriptResponseVersionsInner("", _params: new List<ScriptParameter>(), version: version, isDraft: isDraft, dateUpdated: dateUpdated, dateCreated: dateCreated));
        var outputScript = new GetScriptResponseOutput(m_GetScriptResponse);
        var versions = (List<int?>)outputScript.Versions;
        Assert.AreEqual(1, versions.Count);
        Assert.AreEqual(3, versions.First());
    }

    [Test]
    public void OutputScriptToStringReturnFormattedString()
    {
        var outputScript = new GetScriptResponseOutput(m_GetScriptResponse);
        var outputScriptString = outputScript.ToString();
        var lines = new[]
        {
            "name: Test",
            "language: JS",
            "type: API",
            "versions: []",
            "activeScript:",
            "  version: 0",
            "  datePublished: 0001-01-01T00:00:00",
            "  params: []",
            $"  code: ''{System.Environment.NewLine}"
        };
        var expectedString = string.Join(System.Environment.NewLine, lines);

        Assert.AreEqual(expectedString, outputScriptString);
    }

    const string k_ScriptReleaseTagKey = "releases.cloud.unity.com/aaedbb7b-4047-46bf-b20d-0f16bf1a9a9b";

    // code and params are required by the contract, so the generated constructor rejects null even
    // where a test does not care about them.
    static GetScriptResponseVersionsInner ScriptVersion(
        int? version,
        bool isDraft,
        DateTime dateUpdated,
        DateTime dateCreated = default,
        Dictionary<string, string>? tags = null,
        string code = "module.exports = () => {};")
    {
        var inner = new GetScriptResponseVersionsInner(
            code,
            _params: new List<ScriptParameter>(),
            version: version ?? 1,
            isDraft: isDraft,
            tags: tags,
            dateUpdated: dateUpdated,
            dateCreated: dateCreated);

        // The working copy's version is null, which the required-property check in the constructor
        // will not accept, so it is assigned afterwards.
        if (version is null)
        {
            inner._Version = null;
        }

        return inner;
    }

    static GetScriptResponse ScriptWithVersions(params GetScriptResponseVersionsInner[] versions)
    {
        return new GetScriptResponse(
            "Test",
            "API",
            "JS",
            new GetScriptResponseActiveScript("", 2, _params: new List<ScriptParameter>()),
            new List<GetScriptResponseVersionsInner>(versions),
            new List<ScriptParameter>());
    }

    [Test]
    public void ConstructOutputScript_DefaultShapeIsBareVersionNumbers()
    {
        var response = ScriptWithVersions(
            ScriptVersion(version: null, isDraft: true, dateUpdated: DateTime.UtcNow),
            ScriptVersion(version: 2, isDraft: false, dateUpdated: DateTime.UtcNow),
            ScriptVersion(version: 1, isDraft: false, dateUpdated: DateTime.UtcNow));

        var output = new GetScriptResponseOutput(response);
        var versions = (List<int?>)output.Versions;

        // The working copy has no version number, so the default shape has never included it.
        Assert.That(versions, Is.EqualTo(new List<int?> { 2, 1 }));
        Assert.That(output.ToString(), Does.Not.Contain("isDraft"));
    }

    [Test]
    public void ConstructOutputScript_DetailedVersions_IncludeWorkingCopyAndFields()
    {
        var updated = new DateTime(2026, 9, 8, 11, 47, 37, DateTimeKind.Utc);
        var created = new DateTime(2026, 9, 1, 9, 14, 2, DateTimeKind.Utc);
        var response = ScriptWithVersions(
            ScriptVersion(version: null, isDraft: true, dateUpdated: updated),
            ScriptVersion(
                version: 2,
                isDraft: false,
                dateUpdated: updated,
                dateCreated: created,
                tags: new Dictionary<string, string> { { "tier", "a" } }));

        var yaml = new GetScriptResponseOutput(response, detailedVersions: true).ToString();

        Assert.That(yaml, Does.Contain("isDraft: true"));
        Assert.That(yaml, Does.Contain("isDraft: false"));
        Assert.That(yaml, Does.Contain("version: 2"));
        Assert.That(yaml, Does.Contain("dateUpdated: 2026-09-08T11:47:37"));
        Assert.That(yaml, Does.Contain("dateCreated: 2026-09-01T09:14:02"));
        Assert.That(yaml, Does.Contain("tier: a"));

        // datePublished belongs to activeScript, never to a version.
        Assert.That(yaml, Does.Not.Contain("datePublished: 2026"));
        // code and params are per-version too, but code would flood the output.
        Assert.That(yaml, Does.Not.Contain("module.exports"));
    }

    [Test]
    public void ConstructOutputScript_DetailedWorkingCopy_OmitsVersionKey()
    {
        var response = ScriptWithVersions(
            ScriptVersion(version: null, isDraft: true, dateUpdated: DateTime.UtcNow));

        var output = new GetScriptResponseOutput(response, detailedVersions: true);

        // Asserted in both formats, because they are produced by different code paths: YAML through
        // ToDictionary(), JSON through the DTO's properties.
        var row = (Newtonsoft.Json.Linq.JObject)Newtonsoft.Json.Linq.JObject.Parse(
            Newtonsoft.Json.JsonConvert.SerializeObject(output))["Versions"]![0]!;
        Assert.That(row.ContainsKey("Version"), Is.False, "the working copy has no version number");
        Assert.That(row.Value<bool>("IsDraft"), Is.True);

        var yaml = output.ToString();
        Assert.That(yaml, Does.Contain("isDraft: true"));
        // "- version:" would start a version row; a bare "version:" also matches activeScript's own.
        Assert.That(yaml, Does.Not.Contain("- version:"));
    }

    [Test]
    public void ConstructOutputScript_DetailedVersionTags_ExcludeReservedKeys()
    {
        var response = ScriptWithVersions(
            ScriptVersion(
                version: 1,
                isDraft: false,
                dateUpdated: DateTime.UtcNow,
                tags: new Dictionary<string, string>
                {
                    { "tier", "a" },
                    { k_ScriptReleaseTagKey, "true" }
                }));

        var yaml = new GetScriptResponseOutput(response, detailedVersions: true).ToString();

        Assert.That(yaml, Does.Contain("tier: a"));
        Assert.That(yaml, Does.Not.Contain("releases.cloud.unity.com"));
    }

    [Test]
    public void ConstructOutputScript_DetailedVersionsJsonShapeMatchesYaml()
    {
        var updated = new DateTime(2026, 9, 8, 11, 47, 37, DateTimeKind.Utc);
        var response = ScriptWithVersions(
            ScriptVersion(
                version: 1,
                isDraft: false,
                dateUpdated: updated,
                tags: new Dictionary<string, string>
                {
                    { "tier", "a" },
                    { k_ScriptReleaseTagKey, "true" }
                }));

        var json = Newtonsoft.Json.JsonConvert.SerializeObject(
            new GetScriptResponseOutput(response, detailedVersions: true));

        // PascalCase, matching the enclosing document and the module side. YAML stays camelCase.
        Assert.That(json, Does.Contain("\"IsDraft\":false"));
        Assert.That(json, Does.Contain("2026-09-08T11:47:37"));
        Assert.That(json, Does.Contain("tier"));
        Assert.That(json, Does.Not.Contain("releases.cloud.unity.com"));
        Assert.That(json, Does.Not.Contain("module.exports"));
    }
}
