using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Unity.Services.Cli.CloudCode.Exceptions;
using Unity.Services.Cli.CloudCode.Parameters;
using Unity.Services.Cli.CloudCode.UnitTest.ScriptTestCases;

namespace Unity.Services.Cli.CloudCode.UnitTest.Parameters;

[TestFixture]
class CloudCodeScriptParserTests
{
    readonly CloudCodeScriptParser m_CloudCodeScriptParser = new();

    static readonly IEnumerable<ScriptTestCase> k_ValidScripts = new[]
    {
        new ScriptTestCase("Script.js", "ScriptParam.json"),
        new ScriptTestCase("MixedValue.js", "MixedValueParam.json"),
        new ScriptTestCase("NoParameter.js"),
        new ScriptTestCase("Required.js", "RequiredParam.json"),
    };

    static readonly IEnumerable<ScriptTestCase> k_InSecureScripts = new[]
    {
        new ScriptTestCase("AsyncOperation.js"),
        new ScriptTestCase("InfiniteLoop.js"),
        new ScriptTestCase("MemoryAllocation.js"),
        new ScriptTestCase("ReadFile.js"),
    };

    static readonly IEnumerable<ScriptTestCase> k_InvalidParameterScripts = new[]
    {
        new ScriptTestCase("CyclicReference.js"),
        new ScriptTestCase("BigInt.js"),
    };

    [TestCaseSource(nameof(k_ValidScripts))]
    public async Task ParseJsParameterSuccess(ScriptTestCase testCase)
    {
        var result = await m_CloudCodeScriptParser.ParseToScriptParamsJsonAsync(
            testCase.Script, CancellationToken.None);
        Assert.AreEqual(testCase.Param, result);
    }

    [TestCaseSource(nameof(k_InSecureScripts))]
    public void ParseJsParameterInSecureFail(ScriptTestCase testCase)
    {
        Assert.ThrowsAsync<ScriptEvaluationException>(
            async () => await m_CloudCodeScriptParser.ParseToScriptParamsJsonAsync(
                testCase.Script, CancellationToken.None));
    }

    [TestCaseSource(nameof(k_InvalidParameterScripts))]
    public void ParseJsParameterInvalidParameterFail(ScriptTestCase testCase)
    {
        Assert.ThrowsAsync<ScriptEvaluationException>(
            async () => await m_CloudCodeScriptParser.ParseToScriptParamsJsonAsync(
                testCase.Script, CancellationToken.None));
    }
}
