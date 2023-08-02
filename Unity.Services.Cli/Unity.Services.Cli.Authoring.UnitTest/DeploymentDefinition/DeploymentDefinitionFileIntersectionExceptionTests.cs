using Moq;
using NUnit.Framework;
using Unity.Services.Cli.Authoring.DeploymentDefinition;
using Unity.Services.Deployment.Core.Model;

namespace Unity.Services.Cli.Authoring.UnitTest.Service;

class DeploymentDefinitionFileIntersectionExceptionTests
{
    [TestCase(true)]
    [TestCase(false)]
    public void GetIntersectionMessage_CorrectMessageForExcludes(bool isExcludes)
    {
        var dictionary = new Dictionary<IDeploymentDefinition, List<string>>
        {
            {
                CreateMockDdef("test").Object, new List<string>
                {
                    "path/to/file.test"
                }
            }
        };
        var exception = new DeploymentDefinitionFileIntersectionException(dictionary, isExcludes);
        Assert.IsTrue(exception.Message.Contains("exclusions") == isExcludes);
    }

    static Mock<IDeploymentDefinition> CreateMockDdef(string name)
    {
        var mockDdef = new Mock<IDeploymentDefinition>();
        mockDdef.Setup(d => d.Name).Returns(name);
        return mockDdef;
    }

    [Test]
    public void GetIntersectionMessage_IncludesAllFiles()
    {
        var dictionary = new Dictionary<IDeploymentDefinition, List<string>>
        {
            {
                CreateMockDdef("test1").Object, new List<string>
                {
                    "path/to/folder1/fileA.test",
                    "path/to/folder1/fileB.test"
                }
            },
            {
                CreateMockDdef("test2").Object, new List<string>
                {
                    "path/to/folder2/fileA.test",
                    "path/to/folder2/fileB.test"
                }
            }
        };
        var exception = new DeploymentDefinitionFileIntersectionException(dictionary, false);

        foreach (var file in dictionary.Values.SelectMany(filesList => filesList))
        {
            Assert.IsTrue(exception.Message.Contains(file));
        }
    }
}
