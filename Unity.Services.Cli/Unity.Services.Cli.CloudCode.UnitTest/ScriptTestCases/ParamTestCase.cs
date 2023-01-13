using System.Collections.Generic;
using Unity.Services.Gateway.CloudCodeApiV1.Generated.Model;

namespace Unity.Services.Cli.CloudCode.UnitTest.ScriptTestCases;

public class ParamTestCase
{
    public string Param { get; }

    public List<ScriptParameter>? ExpectedParameters { get; }

    public ParamTestCase(string scriptName, List<ScriptParameter>? parameters = null)
    {
        Param = TestResourceReader.ReadResourceFile(scriptName)
            .Replace(System.Environment.NewLine, "")
            .Replace(" ", "");
        ExpectedParameters = parameters;
    }
}
