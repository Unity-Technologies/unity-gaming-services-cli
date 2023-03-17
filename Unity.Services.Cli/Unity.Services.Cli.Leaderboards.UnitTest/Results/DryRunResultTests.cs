using NUnit.Framework;
using Unity.Services.Cli.Authoring.Model;

namespace Unity.Services.Cli.Leaderboards.UnitTest.Results;

[TestFixture]
class DryRunResultTests
{
    [Test]
    public void DryRunResult_ToStringMatches()
    {
        var expectedResult = $"Legend:{System.Environment.NewLine}[name_1 : id_1]{System.Environment.NewLine}[name_2 : id_2]{System.Environment.NewLine}";
        var testObjects = new List<TestObject>()
        {
            new() { Name = "name_1", Id = "id_1", IrrelevantProperty = 1 },
            new () { Name = "name_2", Id = "id_2", IrrelevantProperty = 1 }
        };
        var dryRunResult = new DryRunResult<TestObject>("Legend:", testObjects,
            testObject => $"[{testObject.Name} : {testObject.Id}]");

        Assert.AreEqual(dryRunResult.ToString(), expectedResult);
    }
}

class TestObject
{
    public string? Name { get; set; }
    public string? Id { get; set; }
    public int IrrelevantProperty { get; set; }
}
