using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Unity.Services.Cli.CloudCode.Exceptions;
using Unity.Services.Cli.CloudCode.Parameters;
using Unity.Services.Cli.CloudCode.UnitTest.ScriptTestCases;
using Unity.Services.Cli.Common.Process;

namespace Unity.Services.Cli.CloudCode.UnitTest.Parameters;

[TestFixture]
class CloudCodeScriptParserTests
{
    static readonly ICliProcess m_CliProcess = new CliProcess();
    static readonly IFile m_File = new FileSystem().File;
    static readonly ICloudScriptParametersParser m_CloudScriptParametersParser = new CloudScriptParametersParser();
    readonly CloudCodeScriptParser m_CloudCodeScriptParser = new(m_CliProcess, m_CloudScriptParametersParser, m_File);

    static readonly IEnumerable<ScriptTestCase> k_ValidScripts = new[]
    {
        new ScriptTestCase("Script.js", "ScriptParam.json"),
        new ScriptTestCase("MixedValue.js", "MixedValueParam.json"),
        new ScriptTestCase("NoParameter.js"),
        new ScriptTestCase("Required.js", "RequiredParam.json"),
        new ScriptTestCase("AsyncOperation.js", "AsyncOperation.json"),
        new ScriptTestCase("HugeFile.js", "HugeFile.json"),
        new ScriptTestCase("QuotedString.js", "QuotedString.json"),
    };

    static readonly IEnumerable<ScriptTestCase> k_InSecureScripts = new[]
    {
        new ScriptTestCase("InfiniteLoop.js"),
        new ScriptTestCase("MemoryAllocation.js"),
        new ScriptTestCase("ReadFile.js"),
    };

    static readonly IEnumerable<ScriptTestCase> k_InvalidParameterScripts = new[]
    {
        new ScriptTestCase("CyclicReference.js"),
        new ScriptTestCase("BigInt.js"),
    };

    [SetUp]
    [TearDown]
    public void DeleteCloudCodeConfigPath()
    {
        if (Directory.Exists(CloudCodeScriptParser.CloudCodePath))
        {
            Directory.Delete(CloudCodeScriptParser.CloudCodePath, true);
        }
    }

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
