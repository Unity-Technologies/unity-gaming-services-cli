using NUnit.Framework;
using Unity.Services.Cli.Observability.Model;
using Unity.Services.Gateway.ObservabilityApiV1.Generated.Model;

namespace Unity.Services.Cli.Observability.UnitTest.Model;

[TestFixture]
class GetLogsResponseOutputTests
{
    [Test]
    public void Constructor_MapsResponseFields()
    {
        var response = new LogsResponse(5, 100, 42, new List<LogRecord>
        {
            new(
                "2023-09-06T10:02:24.904Z",
                "Error",
                17,
                "Hello, world!",
                new Dictionary<string, object> { { "service.name", "cloud-code" } },
                new Dictionary<string, object> { { "log.type", "script" } })
        });

        var output = new GetLogsResponseOutput(response);

        Assert.AreEqual(5, output.Offset);
        Assert.AreEqual(100, output.Limit);
        Assert.AreEqual(42, output.Total);
        Assert.AreEqual(1, output.Results.Count);
        Assert.AreEqual("Error", output.Results[0].SeverityText);
        Assert.AreEqual("Hello, world!", output.Results[0].Body);
    }

    [Test]
    public void Constructor_NullResults_ProducesEmptyList()
    {
        var response = new LogsResponse(0, 100, 0, null);

        var output = new GetLogsResponseOutput(response);

        Assert.IsNotNull(output.Results);
        Assert.AreEqual(0, output.Results.Count);
    }

    [Test]
    public void ToString_ReturnsIndentedJson()
    {
        var response = new LogsResponse(0, 100, 0, new List<LogRecord>());

        var output = new GetLogsResponseOutput(response).ToString();

        StringAssert.Contains("\"Offset\": 0", output);
        StringAssert.Contains("\"Results\": []", output);
    }
}
