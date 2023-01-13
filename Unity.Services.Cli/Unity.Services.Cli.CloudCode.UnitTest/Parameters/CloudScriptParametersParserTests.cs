using System.Collections.Generic;
using Newtonsoft.Json;
using NUnit.Framework;
using Unity.Services.Cli.CloudCode.Exceptions;
using Unity.Services.Cli.CloudCode.Parameters;
using Unity.Services.Cli.CloudCode.UnitTest.ScriptTestCases;
using Unity.Services.Gateway.CloudCodeApiV1.Generated.Model;

namespace Unity.Services.Cli.CloudCode.UnitTest.Parameters;

[TestFixture]
class CloudScriptParametersParserTests
{
    static readonly IEnumerable<ParamTestCase> k_ValidParams = new[]
    {
        new ParamTestCase(
            "ScriptParam.json",
            new List<ScriptParameter>
            {
                new("sides", ScriptParameter.TypeEnum.NUMERIC),
            }),
        new ParamTestCase(
            "RequiredParam.json",
            new List<ScriptParameter>
            {
                new("bleu", ScriptParameter.TypeEnum.STRING, true),
            }),
        new ParamTestCase(
            "MixedValueParam.json",
            new List<ScriptParameter>
            {
                new("bleu", ScriptParameter.TypeEnum.STRING, true),
                new("noir", ScriptParameter.TypeEnum.ANY),
                new("rouge", ScriptParameter.TypeEnum.JSON),
            }),
    };

    static readonly IEnumerable<ParamTestCase> k_InvalidParams = new[]
    {
        new ParamTestCase("ParamInvalidType.json"),
        new ParamTestCase("ParamInvalidRequired.json"),
    };

    readonly CloudScriptParametersParser m_CloudScriptParametersParser = new();

    [TestCaseSource(nameof(k_ValidParams))]
    public void ParseJsParameterSuccess(ParamTestCase testCase)
    {
        var scriptParameters = m_CloudScriptParametersParser.ParseToScriptParameters(testCase.Param);
        CollectionAssert.AreEqual(testCase.ExpectedParameters, scriptParameters);
    }

    [TestCaseSource(nameof(k_InvalidParams))]
    public void ParseInvalidJsParameterThrowException(ParamTestCase testCase)
    {
        Assert.Throws<ScriptEvaluationException>(() => m_CloudScriptParametersParser.ParseToScriptParameters(testCase.Param));
    }
}
