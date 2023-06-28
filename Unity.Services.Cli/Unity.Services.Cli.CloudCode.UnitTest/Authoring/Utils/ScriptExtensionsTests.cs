using System.Linq;
using System.Text;
using NUnit.Framework;
using Unity.Services.Cli.CloudCode.Deploy;
using Unity.Services.Cli.CloudCode.UnitTest.Utils;

namespace Unity.Services.Cli.CloudCode.UnitTest;

[TestFixture]
class ScriptExtensionsTests
{
    [Test]
    public void InjectParametersToBodyDoesAppendParametersToBody()
    {
        var script = new CloudCodeScript
        {
            Body = "// Body",
            Parameters = TestValues.ValidParameters.ToList(),
        };
        var builder = new StringBuilder();
        var expectedBody = script.Body
            + System.Environment.NewLine
            + $"module.exports.params = {TestValues.ValidParametersToJavaScript};"
            + $"{System.Environment.NewLine}";

        script.InjectJavaScriptParametersToBody(builder);

        Assert.That(script.Body, Is.EqualTo(expectedBody));
    }
}
