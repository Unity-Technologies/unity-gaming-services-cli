using System;
using System.Collections.Generic;
using System.Text;
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

    static readonly IReadOnlyList<string> k_Fetched = new[]
    {
        "thing1"
    };

    static readonly IReadOnlyList<string> k_Failed = new[]
    {
        "thing2"
    };

    static readonly List<TestCaseData> k_AppendResultTestData = new()
    {
        new TestCaseData(k_Updated, FetchResult.UpdatedHeader),
        new TestCaseData(k_Created, FetchResult.CreatedHeader),
        new TestCaseData(k_Deleted, FetchResult.DeletedHeader),
        new TestCaseData(k_Failed, FetchResult.FailedHeader),
        new TestCaseData(k_Fetched, FetchResult.FetchedHeader),
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

        Assert.Multiple(
            () =>
            {
                AssertStringifiedResult(result, k_Updated, FetchResult.UpdatedHeader);
                AssertStringifiedResult(result, k_Created, FetchResult.CreatedHeader);
                AssertStringifiedResult(result, k_Deleted, FetchResult.DeletedHeader);
                AssertStringifiedResult(result, k_Failed, FetchResult.FailedHeader);
                AssertStringifiedResult(result, k_Fetched, FetchResult.FetchedHeader);
            });
    }

    [Test]
    public void ToStringFormatsNoContentFetched()
    {
        var fetchResult = new FetchResult(
            Array.Empty<string>(),
            Array.Empty<string>(),
            Array.Empty<string>(),
            Array.Empty<string>(),
            Array.Empty<string>());
        var result = fetchResult.ToString();

        Assert.IsFalse(result.Contains(FetchResult.FetchedHeader));
        Assert.IsTrue(result.Contains(FetchResult.EmptyFetchMessage));
    }

    [TestCaseSource(nameof(k_AppendResultTestData))]
    public void AppendResultBuildsResultAsExpected(ICollection<string> results, string header)
    {
        var builder = new StringBuilder();

        FetchResult.AppendResult(builder, results, header);

        var stringifiedResult = builder.ToString();
        Assert.Multiple(() => AssertStringifiedResult(stringifiedResult, results, header));
    }

    static void AssertStringifiedResult(string stringifiedResult, IEnumerable<string> results, string header)
    {
        Assert.That(stringifiedResult, Contains.Substring(header));
        foreach (var result in results)
        {
            Assert.That(stringifiedResult, Contains.Substring($"    {result}"));
        }
    }
}
