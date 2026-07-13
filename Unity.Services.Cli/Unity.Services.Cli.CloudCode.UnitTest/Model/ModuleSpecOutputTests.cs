using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using Unity.Services.Cli.CloudCode.Model;

namespace Unity.Services.Cli.CloudCode.UnitTest.Model;

[TestFixture]
public class ModuleSpecOutputTests
{
    const string k_SampleYaml = @"openapi: 3.0.1
info:
  title: TestModule OpenApi Specification
  version: 1.0.0
paths:
  /SayHello:
    post:
      description: SayHello POST";

    [Test]
    public void ToString_ReturnsRawYaml()
    {
        var output = new ModuleSpecOutput(k_SampleYaml);

        Assert.That(output.ToString(), Is.EqualTo(k_SampleYaml));
    }

    [Test]
    public void JsonSerialize_ProducesValidJson()
    {
        var output = new ModuleSpecOutput(k_SampleYaml);

        var json = JsonConvert.SerializeObject(output, Formatting.Indented);
        var parsed = JObject.Parse(json);

        Assert.That(parsed["openapi"]?.ToString(), Is.EqualTo("3.0.1"));
        Assert.That(parsed["info"]?["title"]?.ToString(), Is.EqualTo("TestModule OpenApi Specification"));
        Assert.That(parsed["paths"]?["/SayHello"]?["post"]?["description"]?.ToString(), Is.EqualTo("SayHello POST"));
    }

    [Test]
    public void JsonSerialize_NullOutput_WritesNull()
    {
        ModuleSpecOutput? output = null;

        var json = JsonConvert.SerializeObject(output);

        Assert.That(json, Is.EqualTo("null"));
    }

    [Test]
    public void ReadJson_ThrowsNotImplementedException()
    {
        var json = @"{""openapi"": ""3.0.1""}";

        Assert.Throws<NotImplementedException>(
            () => JsonConvert.DeserializeObject<ModuleSpecOutput>(json));
    }
}
