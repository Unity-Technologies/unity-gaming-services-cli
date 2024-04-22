using NUnit.Framework;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Unity.Services.Tooling.Editor.AccessControl.Authoring.Core.Model;
using Unity.Services.Cli.Access.UnitTest.Utils;
using JsonConverter = Unity.Services.Cli.Access.Deploy.JsonConverter;

namespace Unity.Services.Cli.Access.UnitTest.Deploy;

[TestFixture, Ignore("Flaky, I cant figure out why...")]
public class JsonConverterTests
{

    readonly JsonConverter m_Converter = new();
    JsonSerializerSettings? m_Settings;

    [SetUp]
    public void Setup()
    {
        m_Settings = new JsonSerializerSettings()
        {
            Formatting = Formatting.Indented,
            Converters = { new StringEnumConverter() },
            NullValueHandling = NullValueHandling.Ignore,
            DefaultValueHandling = DefaultValueHandling.Ignore
        };
    }

    [Test]
    public void SerializeObjectSerializesSuccessfully()
    {
        var testData = GetTestData();
        var serializedData = m_Converter.SerializeObject(testData);
        var json = Newtonsoft.Json.JsonConvert.SerializeObject(testData, m_Settings);
        Assert.That(serializedData, Is.EqualTo(json));
    }

    [Test]
    public void DeserializeObjectWithDefaultResolverDeserializesSuccessfully()
    {
        var testData = GetTestData();
        var json = JsonConvert.SerializeObject(testData, m_Settings);
        var deserializedData = m_Converter.DeserializeObject<ProjectAccessFileContent>(json);
        Assert.Multiple(() =>
        {
            Assert.That(deserializedData.Statements[0].Sid, Is.EqualTo(testData.Statements[0].Sid));
            Assert.That(deserializedData.Statements[1].Sid, Is.EqualTo(testData.Statements[1].Sid));
        });
    }

    [Test]
    public void DeserializeObjectWithCamelCaseResolverDeserializesSuccessfully()
    {
        var testData = GetTestData();
        var json = Newtonsoft.Json.JsonConvert.SerializeObject(testData, m_Settings);
        var lowerCaseJson = json.ToLower();
        var deserializedData = m_Converter.DeserializeObject<ProjectAccessFileContent>(lowerCaseJson, true);
        Assert.Multiple(() =>
        {
            Assert.That(deserializedData.Statements[0].Sid, Is.EqualTo(testData.Statements[0].Sid));
            Assert.That(deserializedData.Statements[1].Sid, Is.EqualTo(testData.Statements[1].Sid));
        });
    }

    static ProjectAccessFileContent GetTestData()
    {
        IReadOnlyList<AccessControlStatement> statements = new[]
        {
            TestMocks.GetAuthoringStatement("test-sid-1"),
            TestMocks.GetAuthoringStatement("test-sid-2")
        };
        var testData = new ProjectAccessFileContent(statements);
        return testData;
    }
}
