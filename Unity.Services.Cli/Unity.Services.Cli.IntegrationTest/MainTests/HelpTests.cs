using System.Text.RegularExpressions;
using System.Threading.Tasks;
using NUnit.Framework;
using Unity.Services.Cli.Common.Exceptions;
using Unity.Services.Cli.IntegrationTest.Common;

namespace Unity.Services.Cli.IntegrationTest;

public class HelpTests : UgsCliFixture
{
    static object[] s_HelpTestCases =
    {
        new object[] { "-h", $"Usage:\\s*{Regex.Escape("ugs [command] [options]")}" },
        new object[] { "config -h", $"Usage:\\s*{Regex.Escape("ugs config [command] [options]")}" },
        new object[] { "env -h", $"Usage:\\s*{Regex.Escape("ugs env [command] [options]")}" },
    };

    [TestCaseSource(nameof(s_HelpTestCases))]
    public async Task HelpCommandContains(string arguments, string regexOutput)
    {
        await new UgsCliTestCase()
            .Command(arguments)
            .AssertNoErrors()
            .AssertStandardOutput(output => StringAssert.IsMatch(regexOutput, output))
            .ExecuteAsync();
    }

    [Test]
    public async Task NoArgumentsShowsMainHelp()
    {
        await new UgsCliTestCase()
            .Command("")
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardOutput(output => StringAssert.IsMatch($"Usage:\\s*{Regex.Escape("ugs [command] [options]")}", output))
            .AssertStandardError(error => Assert.AreEqual("Required command was not provided.", error.Trim()))
            .ExecuteAsync();
    }
}
