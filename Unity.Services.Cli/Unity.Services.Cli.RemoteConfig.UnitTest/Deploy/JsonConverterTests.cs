using Newtonsoft.Json;
using NUnit.Framework;
using JsonConverter = Unity.Services.Cli.RemoteConfig.Deploy.JsonConverter;

namespace Unity.Services.Cli.RemoteConfig.UnitTest.Deploy;

[TestFixture]
class JsonConverterTests
{
    readonly TestData m_TestData = new()
    {
        Name = "foo",
        Number = 1794806,
        Flag = true,
    };
    readonly string m_ExpectedJson;
    readonly JsonConverter m_Converter = new();

    public JsonConverterTests()
    {
        m_ExpectedJson = Newtonsoft.Json.JsonConvert.SerializeObject(m_TestData, Formatting.Indented);
    }

    [Test]
    public void SerializeObjectSerializesSuccessfully()
    {
        var serializedData = m_Converter.SerializeObject(m_TestData);

        Assert.That(serializedData, Is.EqualTo(m_ExpectedJson));
    }

    [Test]
    public void DeserializeObjectWithDefaultResolverDeserializesSuccessfully()
    {
        var deserializedData = m_Converter.DeserializeObject<TestData>(m_ExpectedJson);

        Assert.That(deserializedData, Is.EqualTo(m_TestData));
    }

    [Test]
    public void DeserializeObjectWithCamelCaseResolverDeserializesSuccessfully()
    {
        var lowerCaseJson = m_ExpectedJson.ToLower();
        var deserializedData = m_Converter.DeserializeObject<TestData>(lowerCaseJson, true);

        Assert.That(deserializedData, Is.EqualTo(m_TestData));
    }
}
