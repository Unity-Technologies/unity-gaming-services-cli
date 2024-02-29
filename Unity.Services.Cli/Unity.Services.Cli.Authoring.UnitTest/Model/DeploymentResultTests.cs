using NUnit.Framework;
using Unity.Services.Cli.Authoring.Model;
using Unity.Services.Cli.Authoring.Model.TableOutput;
using Unity.Services.DeploymentApi.Editor;

namespace Unity.Services.Cli.Authoring.UnitTest.Model;

[TestFixture]
public class DeploymentResultTests
{
    DeploymentResult? m_DeploymentResult;

    static readonly IReadOnlyList<DeployContent> k_DeployedContents = new[]
    {
        new DeployContent("script.js", "Cloud Code", "path", 100, "Published"),
    };

    static readonly IReadOnlyList<DeployContent> k_FailedContents = new[]
    {
        new DeployContent("invalid1.rc", "Remote Config", "path", 0, "Failed to Load", level: SeverityLevel.Error),
        new DeployContent("invalid2.js", "Cloud Code", "path", 0, "Failed to Load", level: SeverityLevel.Error),
    };

    [Test]
    public void ToStringFormatsDeployedAndFailedResults()
    {
        m_DeploymentResult = new DeploymentResult(
            new List<DeployContent>(),
            new List<DeployContent>(),
            new List<DeployContent>(),
            k_DeployedContents,
            k_FailedContents);
        var result = m_DeploymentResult.ToString();

        StringAssert.Contains($"Successfully deployed the following files:{System.Environment.NewLine}    '{k_DeployedContents.First().Path}'", result);
        StringAssert.Contains($"Failed to deploy:{System.Environment.NewLine}", result);
        foreach (var content in k_FailedContents)
        {
            StringAssert.Contains($"    '{content.Path}' - Status: {content.Status.Message} - {content.Detail}", result);
        }
    }

    [Test]
    public void ToStringFormatsNoContentDeployed()
    {
        m_DeploymentResult = new DeploymentResult(
            new List<DeployContent>(),
            new List<DeployContent>(),
            new List<DeployContent>(),
            new List<DeployContent>(),
            new List<DeployContent>());
        var result = m_DeploymentResult.ToString();

        Assert.IsFalse(result.Contains($"Deployed:{System.Environment.NewLine}    {k_DeployedContents.First().Name}"));
        Assert.IsTrue(result.Contains("No content deployed"));
    }

    [Test]
    public void ToTableFormat()
    {
        m_DeploymentResult = new DeploymentResult(
            k_DeployedContents,
            new List<DeployContent>(),
            new List<DeployContent>(),
            k_DeployedContents,
            k_FailedContents);
        var result = m_DeploymentResult.ToTable();
        var expected = new TableContent();

        expected.AddRows(k_DeployedContents.Select(RowContent.ToRow).ToList());
        expected.AddRows(k_FailedContents.Select(RowContent.ToRow).ToList());

        Assert.IsTrue(result.Result.Count == expected.Result.Count);

        for (int i = 0; i < result.Result.Count; i++)
        {
            for (int j = 0; j < result.Result.Count;j++)
            {
                Assert.AreEqual(expected.Result[i].Name, result.Result[i].Name);
            }
        }
    }
}
