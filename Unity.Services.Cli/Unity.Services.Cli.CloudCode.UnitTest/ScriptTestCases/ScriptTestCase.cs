using System.IO;

namespace Unity.Services.Cli.CloudCode.UnitTest.ScriptTestCases;

class ScriptTestCase
{
    public string Script { get; }
    public string? Param { get; }
    public string ScriptPath { get; }

    public ScriptTestCase(string scriptName, string? paramName = null)
    {
        Script = TestResourceReader.ReadResourceFile(scriptName);
        ScriptPath = Path.Combine(Directory.GetCurrentDirectory(), scriptName);
        File.WriteAllText(ScriptPath, Script);
        if (paramName != null)
        {
            Param = TestResourceReader.ReadResourceFile(paramName)
                .Replace(System.Environment.NewLine, "")
                .Replace(" ", "");
        }
    }
}
