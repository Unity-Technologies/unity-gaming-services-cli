using NUnit.Framework;
using Unity.Services.Cli.Authoring.DeploymentDefinition;
using Unity.Services.Cli.Authoring.Model;

namespace Unity.Services.Cli.Authoring.UnitTest.DeploymentDefinition;

[TestFixture]
class DeploymentDefinitionFactoryTest
{
    DeploymentDefinitionFactory m_Factory = null!;
    List<string> m_TempFiles = null!;

    [SetUp]
    public void SetUp()
    {
        m_Factory = new DeploymentDefinitionFactory();
        m_TempFiles = new List<string>();
    }

    [TearDown]
    public void TearDown()
    {
        foreach (var file in m_TempFiles)
        {
            if (File.Exists(file))
                File.Delete(file);
        }
    }

    string CreateTempJsonFile(string jsonContent)
    {
        var tempFile = Path.GetTempFileName();
        m_TempFiles.Add(tempFile);
        File.WriteAllText(tempFile, jsonContent);
        return tempFile;
    }

    [Test]
    public void CreateDeploymentDefinition_WithExtraProperties_PopulatesAdditionalProperties()
    {
        // Arrange
        var json = @"{
            ""name"": ""TestDeployment"",
            ""path"": ""/test/path"",
            ""excludePaths"": [""*.tmp""],
            ""customField"": ""customValue"",
            ""version"": ""1.0.0"",
            ""metadata"": { ""key"": ""value"" }
        }";
        var filePath = CreateTempJsonFile(json);

        // Act
        var ddef = m_Factory.CreateDeploymentDefinition(filePath) as CliDeploymentDefinition;

        // Assert
        Assert.IsNotNull(ddef);
        Assert.AreEqual("TestDeployment", ddef!.Name);
        Assert.AreEqual(filePath, ddef.Path); // Path is the file path, not from JSON
        Assert.AreEqual(1, ddef.ExcludePaths.Count);
        Assert.AreEqual("*.tmp", ddef.ExcludePaths[0]);

        // Verify AdditionalProperties contains the extra fields (including "path" from JSON)
        Assert.AreEqual(4, ddef.AdditionalProperties.Count);
        Assert.IsTrue(ddef.AdditionalProperties.ContainsKey("path")); // JSON path goes to AdditionalProperties
        Assert.IsTrue(ddef.AdditionalProperties.ContainsKey("customField"));
        Assert.IsTrue(ddef.AdditionalProperties.ContainsKey("version"));
        Assert.IsTrue(ddef.AdditionalProperties.ContainsKey("metadata"));

        Assert.AreEqual("/test/path", ddef.AdditionalProperties["path"]); // JSON path value
        Assert.AreEqual("customValue", ddef.AdditionalProperties["customField"]);
        Assert.AreEqual("1.0.0", ddef.AdditionalProperties["version"]);

        // Verify nested object (now Dictionary, not JObject)
        var metadata = ddef.AdditionalProperties["metadata"] as Dictionary<string, object>;
        Assert.IsNotNull(metadata);
        Assert.AreEqual("value", metadata!["key"]);
    }

    [Test]
    public void CreateDeploymentDefinition_WithVariantTags_ConvertsToListOfStrings()
    {
        // Arrange
        var json = @"{
            ""name"": ""VariantTagsTest"",
            ""variantTags"": [""ios"", ""android"", ""web""]
        }";
        var filePath = CreateTempJsonFile(json);

        // Act
        var ddef = m_Factory.CreateDeploymentDefinition(filePath) as CliDeploymentDefinition;

        // Assert
        Assert.IsNotNull(ddef);
        var variantTags = ddef!.AdditionalProperties["variantTags"];

        Assert.IsInstanceOf<List<object>>(variantTags);

        var tagsList = variantTags as List<object>;
        Assert.AreEqual(3, tagsList!.Count);
        Assert.AreEqual("ios", tagsList[0]);
        Assert.AreEqual("android", tagsList[1]);
        Assert.AreEqual("web", tagsList[2]);

        Assert.IsInstanceOf<string>(tagsList[0]);
        Assert.IsInstanceOf<string>(tagsList[1]);
        Assert.IsInstanceOf<string>(tagsList[2]);
    }
}
