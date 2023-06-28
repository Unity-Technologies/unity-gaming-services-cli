using Moq;
using NUnit.Framework;
using NUnit.Framework.Constraints;
using Unity.Services.Cli.CloudCode.Authoring;
using Unity.Services.Cli.CloudCode.Deploy;
using Unity.Services.CloudCode.Authoring.Editor.Core.Model;

namespace Unity.Services.Cli.CloudCode.UnitTest;

[TestFixture]
class CloudCodeScriptNameComparerTests
{
    readonly CloudCodeScriptNameComparer m_Comparer = new();

    static readonly TestCaseData[] k_EqualsTestData =
    {
        new(null, null, Is.True),
        new(null, new ScriptInfo(new ScriptName("foo")), Is.False),
        new(new ScriptInfo(new ScriptName("foo")), null, Is.False),
        new(
            new CloudCodeScript
            {
                Name = new ScriptName("foo")
            },
            new ScriptInfo(new ScriptName("foo")),
            Is.True),
        new(
            new ScriptInfo(new ScriptName("foo")),
            new ScriptInfo(new ScriptName("bar")),
            Is.False),
        new(
            new ScriptInfo(new ScriptName("foo")),
            new ScriptInfo(new ScriptName("foo")),
            Is.True),
    };

    [TestCaseSource(nameof(k_EqualsTestData))]
    public void EqualsReturnsExpectedResultForGivenParameters(IScript? left, IScript? right, Constraint isExpected)
    {
        var areEqual = m_Comparer.Equals(left, right);

        Assert.That(areEqual, isExpected);
    }

    [Test]
    public void GetHashCodeReturnsScriptNameHashCode()
    {
        var script = new Mock<IScript>();
        var name = new ScriptName("foo.js");
        script.SetupGet(x => x.Name)
            .Returns(name);

        var hashCode = m_Comparer.GetHashCode(script.Object);

        Assert.That(hashCode, Is.EqualTo(name.ToString().GetHashCode()));
    }
}
