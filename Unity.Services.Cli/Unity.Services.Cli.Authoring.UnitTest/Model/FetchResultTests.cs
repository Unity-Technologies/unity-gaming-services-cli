using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Unity.Services.Cli.Authoring.Model;

namespace Unity.Services.Cli.Authoring.UnitTest.Model;

[TestFixture]
public class FetchResultTests
{
    static readonly IReadOnlyList<string> k_Updated = new[]
    {
        "thing1",
        "thing2"
    };

    static readonly IReadOnlyList<string> k_Deleted = new[]
    {
        "thing3"
    };

    static readonly IReadOnlyList<string> k_Created = new[]
    {
        "thing4"
    };

    static readonly IReadOnlyList<string> k_Fetched= new[]
    {
        "thing1"
    };

    static readonly IReadOnlyList<string> k_Failed= new[]
    {
        "thing2"
    };


    [Test]
    public void ToStringFormatsFetchedAndFailedResults()
    {
        var fetchResult = new FetchResult(
            k_Updated,
            k_Deleted,
            k_Created,
            k_Fetched,
            k_Failed);
        var result = fetchResult.ToString();

        Assert.IsTrue(result.Contains($"Successfully fetched into the following files:{System.Environment.NewLine}    {k_Fetched[0]}"));
        foreach (var fetchedFile in k_Failed)
        {
            var expected = $"Failed to fetch:{System.Environment.NewLine}    '{fetchedFile}'";
            Assert.IsTrue(result.Contains(expected),
                $"Missing or incorrect log for '{fetchedFile}'") ;
        }
    }


    [Test]
    public void ToStringFormatsNoContentDeployed()
    {
        var fetchResult = new FetchResult(
            Array.Empty<string>(),
            Array.Empty<string>(),
            Array.Empty<string>(),
            Array.Empty<string>(),
            Array.Empty<string>());
        var result = fetchResult.ToString();

        Assert.IsFalse(result.Contains($"Successfully fetched the following contents:{System.Environment.NewLine}"));
        Assert.IsTrue(result.Contains("No content fetched"));
    }
}
