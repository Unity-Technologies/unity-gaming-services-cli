using System;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using Unity.Services.Cli.CloudCode.Exceptions;
using Unity.Services.Cli.CloudCode.Parameters;
using Unity.Services.CloudCode.Authoring.Editor.Core.Model;
using Unity.Services.Gateway.CloudCodeApiV1.Generated.Model;

namespace Unity.Services.Cli.CloudCode.UnitTest.Utils;

[TestFixture]
class CloudCodeScriptParserExtensionsTests
{
    const string k_ScriptName = "bar.js";
    const string k_ScriptBody = "// Script body doesn't matter since we're using Moq.";

    readonly Mock<ICloudCodeScriptParser> m_Parser = new();
    readonly Mock<IScript> m_Script = new();

    [SetUp]
    public void SetUp()
    {
        m_Parser.Reset();
        m_Script.Reset();

        m_Script.Setup(x => x.Name)
            .Returns(new ScriptName(k_ScriptName));
        m_Script.Setup(x => x.Body)
            .Returns(k_ScriptBody);
    }

    [Test]
    public async Task TryParseScriptParametersAsyncReturnsTrueWhenParsingSucceeds()
    {
        m_Parser.Setup(x => x.ParseScriptParametersAsync(k_ScriptBody, CancellationToken.None))
            .ReturnsAsync(
                new[]
                {
                    new ScriptParameter("foo")
                });

        var (hasParameters, errorMessage) = await m_Parser.Object.TryParseScriptParametersAsync(
            m_Script.Object, CancellationToken.None);

        Assert.Multiple(AssertResults);

        void AssertResults()
        {
            Assert.That(hasParameters, Is.True);
            Assert.That(errorMessage, Is.Null);
        }
    }

    [Test]
    public async Task TryParseScriptParametersAsyncSetsErrorMessageWhenParsingThrows()
    {
        const string exceptionMessage = "This is a test error message.";
        var expectedErrorMessage = string.Format(
            CloudCodeScriptParserExtensions.ErrorMessageFormat,
            k_ScriptName,
            System.Environment.NewLine,
            exceptionMessage);
        m_Parser.Setup(x => x.ParseScriptParametersAsync(k_ScriptBody, CancellationToken.None))
            .ThrowsAsync(new ScriptEvaluationException(exceptionMessage));

        var (hasParameters, errorMessage) = await m_Parser.Object.TryParseScriptParametersAsync(
            m_Script.Object, CancellationToken.None);

        Assert.Multiple(AssertResults);

        void AssertResults()
        {
            Assert.That(hasParameters, Is.False);
            Assert.That(errorMessage, Is.EqualTo(expectedErrorMessage));
        }
    }

    [Test]
    public void TryParseScriptParametersAsyncThrowsNonScriptEvaluationException()
    {
        m_Parser.Setup(x => x.ParseScriptParametersAsync(k_ScriptBody, CancellationToken.None))
            .ThrowsAsync(new Exception("This mustn't be caught."));

        Assert.That(TryParseScriptParametersAsync, Throws.Exception);

        async Task TryParseScriptParametersAsync()
            => await m_Parser.Object.TryParseScriptParametersAsync(m_Script.Object, CancellationToken.None);
    }
}
