using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using Unity.Services.Cli.Authoring.Model;
using Unity.Services.Cli.CloudCode.Deploy;
using Unity.Services.Cli.CloudCode.Exceptions;
using Unity.Services.Cli.CloudCode.Model;
using Unity.Services.Cli.CloudCode.Parameters;
using Unity.Services.Cli.CloudCode.Service;
using Unity.Services.Cli.CloudCode.Utils;
using Unity.Services.Gateway.CloudCodeApiV1.Generated.Model;

namespace Unity.Services.Cli.CloudCode.UnitTest.Deploy;

[TestFixture]
class CloudCodeScriptsLoaderTests
{
    static readonly Mock<ICloudCodeInputParser> m_MockCloudCodeInputParser = new();
    static readonly Mock<ICloudCodeScriptParser> m_MockCloudCodeScriptParser = new();

    readonly CloudCodeScriptsLoader m_CodeScriptsLoader = new();

    [SetUp]
    public void SetUp()
    {
        m_MockCloudCodeInputParser.Reset();
        m_MockCloudCodeScriptParser.Reset();
    }

    [Test]
    public async Task LoadScriptAsyncReturnScriptList()
    {
        var paths = new[]
        {
            "script1.js"
        };
        List<DeployContent> contents = new List<DeployContent>();
        var expectedParameters = new List<ScriptParameter>
        {
            new("sides", ScriptParameter.TypeEnum.NUMERIC)
        };

        const string scriptCode = "module.exports.params = { sides: \"NUMERIC\" };";
        m_MockCloudCodeInputParser.Setup(c => c.LoadScriptCodeAsync("script1.js", CancellationToken.None))
            .ReturnsAsync(scriptCode);
        m_MockCloudCodeScriptParser.Setup(c => c.ParseScriptParametersAsync(scriptCode, CancellationToken.None))
            .ReturnsAsync(new ParseScriptParametersResult(false, expectedParameters));
        var loadResult = await m_CodeScriptsLoader.LoadScriptsAsync(paths, CloudCodeConstants.ServiceTypeScripts, ".js",
            m_MockCloudCodeInputParser.Object, m_MockCloudCodeScriptParser.Object, CancellationToken.None);
        Assert.That(loadResult.LoadedScripts.Count, Is.EqualTo(1));
        Assert.That(loadResult.LoadedScripts[0].Parameters.Count, Is.EqualTo(expectedParameters.Count));
        Assert.That(loadResult.LoadedScripts[0].Parameters[0].Name, Is.EqualTo(expectedParameters[0].Name));
    }

    [Test]
    public void LoadScriptAsyncCatchScriptEvaluationException()
    {
        var paths = new[]
        {
            "script1.js"
        };
        List<DeployContent> contents = new List<DeployContent>();
        var expectedParameters = new List<ScriptParameter>
        {
            new("sides", ScriptParameter.TypeEnum.NUMERIC)
        };

        m_MockCloudCodeInputParser.Setup(c => c.LoadScriptCodeAsync("script1.js", CancellationToken.None))
            .ThrowsAsync(new ScriptEvaluationException("Fail to parse script"));
        Assert.DoesNotThrowAsync(async () =>
            await m_CodeScriptsLoader.LoadScriptsAsync(paths, CloudCodeConstants.ServiceTypeScripts, ".js",
                m_MockCloudCodeInputParser.Object, m_MockCloudCodeScriptParser.Object,
                CancellationToken.None));
    }
}
