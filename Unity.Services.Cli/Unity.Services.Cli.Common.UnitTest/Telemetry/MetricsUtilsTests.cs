using System.CommandLine;
using System.CommandLine.Parsing;
using NUnit.Framework;
using Unity.Services.Cli.Common.Telemetry.AnalyticEvent;

namespace Unity.Services.Cli.Common.UnitTest.Telemetry;

[TestFixture]
public class MetricsUtilsTests
{
    [Test]
    public void ConvertSymbolResultToString_ParsesCorrectly()
    {
        var command = new Command("ugs");
        var subCommand1 = new Command("env");
        var subCommand2 = new Command("list");
        command.AddCommand(subCommand1);
        subCommand1.AddCommand(subCommand2);

        var parser = new Parser(command);
        var result = AnalyticEventUtils.ConvertSymbolResultToString(
            parser.Parse(new[] { command.Name, subCommand1.Name, subCommand2.Name }).CommandResult);
        StringAssert.AreEqualIgnoringCase($"{command.Name}_{subCommand1.Name}_{subCommand2.Name}", result);
    }
}
