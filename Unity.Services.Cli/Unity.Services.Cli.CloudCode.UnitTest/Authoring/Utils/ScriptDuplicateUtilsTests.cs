using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Unity.Services.Cli.CloudCode.Deploy;
using Unity.Services.Cli.CloudCode.Authoring;
using Unity.Services.CloudCode.Authoring.Editor.Core.Model;

namespace Unity.Services.Cli.CloudCode.UnitTest.Authoring.Utils;

[TestFixture]
class ScriptDuplicateUtilsTests
{
    [Test]
    public void FilterDuplicatesUsesIdentifierToFindDuplicates()
    {
        const string duplicateKey = "foo";
        var expectedDuplicates = new List<IScript>
        {
            new CloudCodeScript
            {
                Name = new ScriptName(duplicateKey),
            },
            new CloudCodeScript
            {
                Name = new ScriptName(duplicateKey),
            },
        };
        var expectedFiltered = new List<IScript>
        {
            new CloudCodeScript
            {
                Name = new ScriptName("test"),
            },
        };
        var expectedDuplicateGroups = new Dictionary<string, List<IScript>>
        {
            [duplicateKey] = expectedDuplicates,
        };
        var resources = expectedDuplicates.Union(expectedFiltered).ToList();

        var filteredScripts = resources.FilterDuplicates(out var duplicateGroups, out var duplicates);

        Assert.Multiple(AssertDuplicatesAreFiltered);

        void AssertDuplicatesAreFiltered()
        {
            Assert.That(filteredScripts, Is.EquivalentTo(expectedFiltered));
            Assert.That(duplicates, Is.EquivalentTo(expectedDuplicates));
            Assert.That(duplicateGroups, Is.EquivalentTo(expectedDuplicateGroups));
        }
    }

    [Test]
    public void GetDuplicatesMessageUsesIdentifierToFillMessage()
    {
        const string duplicateKey = "foo";
        var duplicateGroups = new Dictionary<string, List<IScript>>
        {
            [duplicateKey] = new()
            {
                new CloudCodeScript
                {
                    Path = $"foo/{duplicateKey}",
                },
                new CloudCodeScript
                {
                    Path = $"bar/{duplicateKey}",
                },
            },
        };

        var message = duplicateGroups.GetDuplicatesMessage();

        Assert.Multiple(AssertDuplicateInfoAreLogged);

        void AssertDuplicateInfoAreLogged()
        {
            foreach (var (key, duplicates) in duplicateGroups)
            {
                Assert.That(message, Contains.Substring($"Duplicates for \"{key}\" were found:"));
                foreach (var duplicate in duplicates)
                {
                    Assert.That(message, Contains.Substring(duplicate.Path));
                }
            }
        }
    }
}
