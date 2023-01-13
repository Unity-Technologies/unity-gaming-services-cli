using Newtonsoft.Json.Linq;
using NUnit.Framework;
using Unity.Services.Cli.RemoteConfig.Deploy;
using Unity.Services.RemoteConfig.Editor.Authoring.Core.Formatting;

namespace Unity.Services.Cli.RemoteConfig.UnitTest.Deploy;

[TestFixture]
public class ConfigTypeDeriverTests
{
    ConfigTypeDeriver m_ConfigTypeDeriver = new();

    static object[] s_TestCases =
    {
        new object[]
        {
            "test_msg", ConfigType.STRING
        },
        new object[]
        {
            1, ConfigType.INT
        },
        new object[]
        {
            true, ConfigType.BOOL
        },
        new object[]
        {
            1.0f, ConfigType.FLOAT
        },
        new object[]
        {
            1.6, ConfigType.FLOAT
        },
        new object[]
        {
            1L, ConfigType.LONG
        },
        new object[]
        {
            new JObject(), ConfigType.JSON
        },
    };


    [TestCaseSource(nameof(s_TestCases))]
    public void DeriveType_ReturnsCorrectType(object input, int configType)
    {
        var response = m_ConfigTypeDeriver.DeriveType(input);
        Assert.That(response, Is.EqualTo((ConfigType)configType));
    }
}
