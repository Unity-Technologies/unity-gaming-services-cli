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

    static readonly FetchResult k_FetchResultDummy = new FetchResult(Array.Empty<FetchResult>());

    static readonly List<TestCaseData> k_AppendResultTestData = new()
    {
        new TestCaseData(StringsToDeployContent(k_Updated), AuthorResult.UpdatedHeader),
        new TestCaseData(StringsToDeployContent(k_Created), AuthorResult.CreatedHeader),
        new TestCaseData(StringsToDeployContent(k_Deleted), AuthorResult.DeletedHeader),
        new TestCaseData(StringsToDeployContent(k_Failed), k_FetchResultDummy.AuthoredHeader),
        new TestCaseData(StringsToDeployContent(k_Fetched), k_FetchResultDummy.FailedHeader),
    };

    [Test]
    public void ToStringFormatsFetchedAndFailedResults()
    {
        var fetchResult = new FetchResult(
            StringsToDeployContent(k_Updated),
            StringsToDeployContent(k_Deleted),
            StringsToDeployContent(k_Created),
            StringsToDeployContent(k_Fetched),
            StringsToDeployContent(k_Failed),
            true);
        var result = fetchResult.ToString();

        Assert.Multiple(
            () =>
            {
                AssertStringifiedResult(result, k_Updated, AuthorResult.DryUpdatedHeader);
                AssertStringifiedResult(result, k_Created, AuthorResult.DryCreatedHeader);
                AssertStringifiedResult(result, k_Deleted, AuthorResult.DryDeletedHeader);
                AssertStringifiedResult(result, k_Failed, k_FetchResultDummy.DryFailedHeader);
                AssertStringifiedResult(result, k_Fetched, k_FetchResultDummy.DryAuthoredHeader);
            });
    }

    [Test]
    public void ToStringFormatsNoContentFetched()
    {
        var fetchResult = new FetchResult(Array.Empty<FetchResult>());
        var result = fetchResult.ToString();

        Assert.IsFalse(result.Contains(fetchResult.AuthoredHeader));
        Assert.IsTrue(result.Contains(fetchResult.NoActionMessage));
    }

    static void AssertStringifiedResult(string stringifiedResult, IEnumerable<string> results, string header)
    {
        Assert.That(stringifiedResult, Contains.Substring(header));
        foreach (var result in results)
        {
            Assert.That(stringifiedResult, Contains.Substring($"    '{result}'"));
        }
    }

    static IReadOnlyList<DeployContent> StringsToDeployContent(IEnumerable<string> strs)
    {
        return strs.Select(s => new DeployContent(s, string.Empty, s)).ToList();
    }
}
