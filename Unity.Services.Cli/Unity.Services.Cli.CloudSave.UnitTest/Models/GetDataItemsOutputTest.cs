using NUnit.Framework;
using Unity.Services.Cli.CloudSave.Models;
using Unity.Services.Gateway.CloudSaveApiV1.Generated.Model;

namespace Unity.Services.Cli.CloudSave.UnitTest.Models;

[TestFixture]
class GetDataItemsOutputTest
{
    static readonly GetItemsResponse k_ValidResponse = new GetItemsResponse(
        new List<Item>()
        {
            new Item("key1", "value1", "writelock1", new ModifiedMetadata(DateTime.MaxValue), new ModifiedMetadata(DateTime.MinValue)),
            new Item("key2", "value2", "writelock2", new ModifiedMetadata(DateTime.MaxValue), new ModifiedMetadata(DateTime.MinValue))
        },
        new GetItemsResponseLinks("nextValue")
    );

    [Test]
    public void GetDataItemsOutput_PrintsExpectedOutput()
    {
        var output = new GetDataItemsOutput(k_ValidResponse);
        var expectedString = @"items:
- key: key1
  value: value1
  writeLock: writelock1
  modified:
    date: 9999-12-31T23:59:59.9999999
  created:
    date: 0001-01-01T00:00:00.0000000
- key: key2
  value: value2
  writeLock: writelock2
  modified:
    date: 9999-12-31T23:59:59.9999999
  created:
    date: 0001-01-01T00:00:00.0000000
next: nextValue
".Replace("\r\n", "\n")
            .Replace("\n", System.Environment.NewLine);
        Assert.That(output.ToString(), Is.EqualTo(expectedString));
    }
}

