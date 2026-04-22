using NUnit.Framework;
using Unity.Services.Cli.Common.Logging;

namespace Unity.Services.Cli.Common.UnitTest.LogFormatter;

[TestFixture]
public class MessageFormatterTests
{
    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public void TryFormatAsJson_WithNullOrEmptyMessage_ReturnsEmptyString(string message)
    {
        var result = MessageFormatter.TryFormatAsJson(message);
        Assert.That(result, Is.EqualTo(string.Empty));
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public void TryFormatAsYaml_WithNullOrEmptyMessage_ReturnsEmptyString(string message)
    {
        var result = MessageFormatter.TryFormatAsYaml(message);
        Assert.That(result, Is.EqualTo(string.Empty));
    }

    [Test]
    public void TryFormatAsYaml_WithSimpleJsonObject_ReturnsYaml()
    {
        const string json = """{"title": "Error", "detail": "Something went wrong"}""";
        var result = MessageFormatter.TryFormatAsYaml(json);

        Assert.That(result, Contains.Substring("Title: Error"));
        Assert.That(result, Contains.Substring("Detail: Something went wrong"));
    }

    [Test]
    public void TryFormatAsYaml_WithNestedObject_ReturnsFormattedYaml()
    {
        const string json = """
                            {
                                "error": {
                                    "code": 400,
                                    "message": "Bad Request"
                                },
                                "timestamp": "2023-01-01T00:00:00Z"
                            }
                            """;

        var result = MessageFormatter.TryFormatAsYaml(json);

        Assert.That(result, Contains.Substring("Error:"));
        Assert.That(result, Contains.Substring("Code: 400"));
        Assert.That(result, Contains.Substring("Message: Bad Request"));
        Assert.That(result, Contains.Substring("Timestamp: 2023-01-01T00:00:00.0000000Z"));
    }

    [Test]
    public void PrettifyYaml_WithArray_ReturnsFormattedYaml()
    {
        const string json = """
                            {
                                "errors": [
                                    {"field": "name", "message": "Required"},
                                    {"field": "email", "message": "Invalid format"}
                                ]
                            }
                            """;

        var result = MessageFormatter.TryFormatAsYaml(json);

        Assert.That(result, Contains.Substring("Errors:"));
        Assert.That(result, Contains.Substring("- Field: name"));
        Assert.That(result, Contains.Substring("Message: Required"));
    }

    [Test]
    public void PrettifyJson_WithValidJsonString_ReturnsFormattedJson()
    {
        const string json = """{"title":"Error","detail":"Something went wrong"}""";
        var result = MessageFormatter.TryFormatAsJson(json);

        Assert.That(result, Contains.Substring("\"title\": \"Error\""));
        Assert.That(result, Contains.Substring("\"detail\": \"Something went wrong\""));
    }

    [Test]
    public void PrettifyJson_WithNonJson_ReturnsOriginalMessage()
    {
        const string malformedJson = "{invalid json";
        var result = MessageFormatter.TryFormatAsJson(malformedJson);

        Assert.That(result, Is.EqualTo(malformedJson));
    }

    [Test]
    public void PrettifyYaml_WithNonJson_ReturnsOriginalMessage()
    {
        const string plainText = "{invalid json";
        var result = MessageFormatter.TryFormatAsYaml(plainText);

        Assert.That(result, Is.EqualTo(plainText));
    }

    [Test]
    public void PrettifyYaml_WithEmptyJsonObject_ReturnsEmptyYaml()
    {
        const string json = "{}";
        var result = MessageFormatter.TryFormatAsYaml(json);

        Assert.That(result, Is.EqualTo("{}" + System.Environment.NewLine));
    }

    [Test]
    public void PrettifyYaml_WithJsonArray_ReturnsYamlArray()
    {
        const string json = """{ "items": ["item1", "item2", "item3"] }""";
        var result = MessageFormatter.TryFormatAsYaml(json);

        Assert.That(result, Contains.Substring("- item1"));
        Assert.That(result, Contains.Substring("- item2"));
        Assert.That(result, Contains.Substring("- item3"));
    }
}
