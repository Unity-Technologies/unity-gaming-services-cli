using NUnit.Framework;
using Unity.Services.Cli.CloudCode.Deploy;
using Unity.Services.CloudCode.Authoring.Editor.Core.Model;
using Unity.Services.Gateway.CloudCodeApiV1.Generated.Model;

namespace Unity.Services.Cli.CloudCode.UnitTest.Deploy;

[TestFixture]
class ScriptParameterExtensionTests
{
    const string k_ParameterName1 = "name1";
    const string k_ParameterName2 = "name2";

    [TestCase(k_ParameterName1, true, ScriptParameter.TypeEnum.ANY, ParameterType.Any)]
    [TestCase(k_ParameterName1, false, ScriptParameter.TypeEnum.ANY, ParameterType.Any)]
    [TestCase(k_ParameterName2, false, ScriptParameter.TypeEnum.ANY, ParameterType.Any)]
    [TestCase(k_ParameterName2, false, ScriptParameter.TypeEnum.BOOLEAN, ParameterType.Boolean)]
    [TestCase(k_ParameterName2, false, ScriptParameter.TypeEnum.JSON, ParameterType.JSON)]
    [TestCase(k_ParameterName2, false, ScriptParameter.TypeEnum.NUMERIC, ParameterType.Numeric)]
    [TestCase(k_ParameterName2, false, ScriptParameter.TypeEnum.STRING, ParameterType.String)]
    [TestCase(k_ParameterName2, false, null, ParameterType.String)]
    public void ToCloudCodeParameterReturnCorrect(string name, bool expectedRequired,
        ScriptParameter.TypeEnum? scriptParamType, ParameterType expectedType)
    {
        var expectedCloudCodeParam = new CloudCodeParameter()
        {
            Name = name,
            ParameterType = expectedType,
            Required = expectedRequired
        };

        var scriptParam = new ScriptParameter(name, scriptParamType, expectedRequired);
        var resultCcParam = scriptParam.ToCloudCodeParameter();
        Assert.AreEqual(expectedCloudCodeParam, resultCcParam);
    }
}
