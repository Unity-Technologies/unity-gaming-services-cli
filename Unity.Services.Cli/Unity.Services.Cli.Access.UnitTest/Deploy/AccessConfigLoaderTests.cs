using System.IO.Abstractions;
using Moq;
using NUnit.Framework;
using Unity.Services.Cli.Access.Deploy;
using Unity.Services.Tooling.Editor.AccessControl.Authoring.Core.Json;
using IFileSystem = Unity.Services.Tooling.Editor.AccessControl.Authoring.Core.IO.IFileSystem;

namespace Unity.Services.Cli.Access.UnitTest.Deploy;

[TestFixture]
public class AccessConfigLoaderTests
{
    AccessConfigLoader? m_AccessConfigLoader;
    readonly Mock<IFileSystem> m_FileSystem = new();
    readonly IJsonConverter m_JsonConverter = new JsonConverter();
    readonly Mock<IPath> m_Path = new();

    [Test]
    public async Task ConfigLoader_Deserializes()
    {
        m_AccessConfigLoader = new AccessConfigLoader(
            m_FileSystem.Object,
            m_Path.Object,
            m_JsonConverter);
        var content = @"{
  'Statements': [
    {
      'Sid': 'allow-access-to-economy',
      'Action': [
        'Read'
      ],
      'Effect': 'Allow',
      'Principal': 'Player',
      'Resource': 'urn:ugs:economy:*',
      'ExpiresAt': '2024-04-29T18:30:51.243Z',
      'Version': '10.0'
    }
  ]
}";
        m_FileSystem.Setup(f => f.ReadAllText(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(content);

        var configs = await m_AccessConfigLoader
            .LoadFilesAsync(
                new[]
                {
                    "path"
                },
                CancellationToken.None);

        var config = configs.Loaded[0];
        Assert.Multiple(() =>
        {
            Assert.That(config.Statements[0].Sid, Is.EqualTo("allow-access-to-economy"));
            Assert.That(config.Statements[0].Version, Is.EqualTo("10.0"));
        });
    }

    [Test]
    public async Task ConfigLoader_DeserializesShouldFail()
    {
        m_AccessConfigLoader = new AccessConfigLoader(
            m_FileSystem.Object,
            m_Path.Object,
            m_JsonConverter);
        var content = @"";

        m_FileSystem.Setup(f => f.ReadAllText(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(content);

        var configs = await m_AccessConfigLoader
            .LoadFilesAsync(
                new[]
                {
                    "path"
                },
                CancellationToken.None);
        Assert.Multiple(() =>
        {
            Assert.That(configs.Failed, Has.Count.EqualTo(1));
            Assert.That(configs.Loaded, Is.Empty);
        });
    }
}
