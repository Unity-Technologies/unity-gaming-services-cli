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
        ScriptType.API,
        Language.JS,
        new GetScriptResponseActiveScript("", 0, _params: new List<ScriptParameter>()),
        new List<GetScriptResponseVersionsInner>(),
        new List<ScriptParameter>());

    [SetUp]
    public void SetUp()
    {
        const string scriptName = "Test";
        const ScriptType scriptType = ScriptType.API;
        const Language language = Language.JS;
        const string code = "";
        const int version = 0;
        var dateTime = new DateTime();
        var parameters = new List<ScriptParameter>();
        var script = new GetScriptResponseActiveScript(code, version, dateTime, parameters);
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
            new GetScriptResponseVersionsInner("", version, isDraft, dateUpdated, dateCreated));
        var outputScript = new GetScriptResponseOutput(m_GetScriptResponse);
        Assert.AreEqual(1, outputScript.Versions.Count);
        Assert.AreEqual(3, outputScript.Versions.First());
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
}
