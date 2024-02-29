using Unity.Services.Cli.GameServerHosting.Services;
using Unity.Services.Gateway.GameServerHostingApiV1.Generated.Model;

namespace Unity.Services.Cli.GameServerHosting.UnitTest.Services;

[TestFixture]
[TestOf(typeof(CoreDumpStateConverter))]
public class CoreDumpStateConverterTest
{
    [TestCase(GetCoreDumpConfig200Response.StateEnum.NUMBER_0, "disabled")]
    [TestCase(GetCoreDumpConfig200Response.StateEnum.NUMBER_1, "enabled")]
    [TestCase((GetCoreDumpConfig200Response.StateEnum)3, "unknown")]
    [TestCase(null, "unknown")]
    public void ConvertToString(GetCoreDumpConfig200Response.StateEnum? state, string expectedString)
    {
        Assert.That(CoreDumpStateConverter.ConvertToString(state), Is.EqualTo(expectedString));
    }
}
