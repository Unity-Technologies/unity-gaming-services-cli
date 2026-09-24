using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using Unity.Services.Cli.Purchasing.Authoring;

namespace Unity.Services.Cli.Purchasing.UnitTest.Authoring;

[TestFixture]
class CatalogItemConfigFileTests
{
    [Test]
    public void Extension_IsUcat()
    {
        var template = new CatalogItemConfigFile();
        Assert.That(template.Extension, Is.EqualTo(".ucat"));
    }

    [Test]
    public void FileBodyText_IsValidJson()
    {
        var template = new CatalogItemConfigFile();
        Assert.DoesNotThrow(() => JsonConvert.DeserializeObject(template.FileBodyText));
    }

    [Test]
    public void FileBodyText_ContainsSchemaProperty()
    {
        var template = new CatalogItemConfigFile();
        var json = JsonConvert.DeserializeObject<JObject>(template.FileBodyText)!;
        Assert.That(
            json.Value<string>("$schema"),
            Is.EqualTo("https://ugs-config-schemas.unity3d.com/v1/purchasing-catalog.schema.json"));
    }
}
