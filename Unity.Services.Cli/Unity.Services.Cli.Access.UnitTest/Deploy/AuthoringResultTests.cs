
using NUnit.Framework;
using Unity.Services.Tooling.Editor.AccessControl.Authoring.Core.Model;
using Unity.Services.Cli.Access.Deploy;
using Unity.Services.DeploymentApi.Editor;

namespace Unity.Services.Cli.Access.UnitTest.Deploy;

[TestFixture]
public class AuthoringResultTestsx
{
    [Test]
    public void Test_Orders_ofComplexitx()
    {
        var statements1 = new List<AccessControlStatement>()
        {
            new() { Sid = "one" },
            new() { Sid = "two" },
            new() { Sid = "three" }
        };

        var statements2 = new List<AccessControlStatement>()
        {
            new() { Sid = "alpha" },
            new() { Sid = "beta" },
            new() { Sid = "gamma" }
        };

        var file1 = new ProjectAccessFile()
        {
            Name = "test-file.ac",
            Statements = statements1
        };

        var file2 = new ProjectAccessFile()
        {
            Name = "test-file.ac",
            Statements = statements2
        };
        var ar = new AccessDeploymentResult(
            statements1.Concat(statements2).ToList(),
                Array.Empty<AccessControlStatement>(),
            Array.Empty<AccessControlStatement>(),
            new []{file1, file2},
            Array.Empty<ProjectAccessFile>()
        );

        var expectedTable = new List<IDeploymentItem>();
        expectedTable.Add(file1);
        expectedTable.AddRange(file1.Statements);
        expectedTable.Add(file2);
        expectedTable.AddRange(file2.Statements);

        var table = ar.ToTable();
        foreach (var (actual,expected) in table.Result.Zip(expectedTable))
        {
            Assert.That(actual.Name, Is.EqualTo(expected.Name));
        }
    }
}
