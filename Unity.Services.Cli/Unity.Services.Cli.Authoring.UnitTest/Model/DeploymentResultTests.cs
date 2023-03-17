using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Unity.Services.Cli.Authoring.Model;

namespace Unity.Services.Cli.Authoring.UnitTest.Model;

[TestFixture]
public class DeploymentResultTests
{
    DeploymentResult? m_DeploymentResult;

    static readonly IReadOnlyCollection<DeployContent> k_DeployedContents = new[]
    {
        new DeployContent("script.js", "Cloud Code", "path", 100, "Published"),
    };

    static readonly IReadOnlyCollection<DeployContent> k_FailedContents = new[]
    {
        new DeployContent("invalid1.rc", "Remote Config", "path", 0, "Failed to Load"),
        new DeployContent("invalid2.js", "Cloud Code", "path", 0, "Failed to Load"),
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

        Assert.IsTrue(result.Contains($"Deployed:{System.Environment.NewLine}    {k_DeployedContents.First().Name}"));
        foreach (var content in k_FailedContents)
        {
            StringAssert.Contains($"Failed to deploy:{System.Environment.NewLine}"+
                                  $"    '{content.Name}' - Status: {content.Status}{System.Environment.NewLine}"+
                                  $"    {content.Detail}", result);
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
}
