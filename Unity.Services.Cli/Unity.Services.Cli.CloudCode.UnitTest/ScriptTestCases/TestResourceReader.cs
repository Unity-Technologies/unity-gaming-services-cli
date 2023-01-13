using System.IO;
using System.Reflection;

namespace Unity.Services.Cli.CloudCode.UnitTest.ScriptTestCases;

static class TestResourceReader
{
    const string k_TestJsNameSpace = "Unity.Services.Cli.CloudCode.UnitTest.ScriptTestCases.JS";

    public static string ReadResourceFile(string fileName)
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream($"{k_TestJsNameSpace}.{fileName}");
        using var reader = new StreamReader(stream!);
        return reader.ReadToEnd();
    }
}
