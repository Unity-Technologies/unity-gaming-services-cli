namespace Unity.Services.Cli.CloudCode.UnitTest.ScriptTestCases;

class ScriptTestCase
{
    public string Script { get; }
    public string? Param { get; }

    public ScriptTestCase(string scriptName)
    {
        Script = TestResourceReader.ReadResourceFile(scriptName);
        Param = null;
    }

    public ScriptTestCase(string scriptName, string paramName)
    {
        Script = TestResourceReader.ReadResourceFile(scriptName);
        Param = TestResourceReader.ReadResourceFile(paramName)
            .Replace(System.Environment.NewLine, "")
            .Replace(" ", "");
    }
}
