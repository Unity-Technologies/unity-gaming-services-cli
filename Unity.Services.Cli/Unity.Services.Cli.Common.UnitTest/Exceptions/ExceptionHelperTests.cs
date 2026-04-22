using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using System.Net;
using System.Text;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using Spectre.Console;
using Spectre.Console.Rendering;
using Unity.Services.Cli.Common.Exceptions;
using Unity.Services.Cli.Common.Telemetry;
using Unity.Services.Cli.TestUtils;
using IdentityApiException = Unity.Services.Gateway.IdentityApiV1.Generated.Client.ApiException;
using CloudCodeApiException = Unity.Services.Gateway.CloudCodeApiV1.Generated.Client.ApiException;

namespace Unity.Services.Cli.Common.UnitTest.Exceptions;

[TestFixture]
class ExceptionHelperTests
{
    static readonly MockHelper k_MockHelper = new();
    InvocationContext? m_Context;
    readonly Mock<IAnsiConsole> m_MockAnsiConsole = new();
    ExceptionHelper? m_ExceptionHelper;
    Parser? m_Parser;

    public static IEnumerable<TestCaseData> ApiExceptionTestCases
    {
        get
        {
            yield return new TestCaseData(new IdentityApiException());
            yield return new TestCaseData(new CloudCodeApiException());
        }
    }

    [SetUp]
    public void SetUp()
    {
        k_MockHelper.MockDiagnostics.Reset();
        m_ExceptionHelper = new(k_MockHelper.MockDiagnostics.Object, m_MockAnsiConsole.Object);
        m_MockAnsiConsole.Reset();
        m_Parser = new Parser(new RootCommand("Test root command"));
        var result = m_Parser.Parse(Array.Empty<string>());
        m_Context = new InvocationContext(result);
    }

    [TearDown]
    public void TearDown()
    {
        k_MockHelper.ClearInvocations();
    }

    [TestCaseSource(nameof(ApiExceptionTestCases))]
    public void HandleApiException(Exception exception)
    {
        Assert.DoesNotThrow(() =>
            m_ExceptionHelper!.HandleException(exception, k_MockHelper.MockLogger.Object, m_Context!));
        TestsHelper.VerifyLoggerWasCalled(k_MockHelper.MockLogger, LogLevel.Error);
        Assert.AreEqual(ExitCode.HandledError, m_Context!.ExitCode);
    }

    [Test]
    public void HandleHandledCliException()
    {
        var expectedExitCode = ExitCode.HandledError;
        var errorMessage = "my error";
        Assert.DoesNotThrow(() => m_ExceptionHelper!.HandleException(
            new CliException(errorMessage, ExitCode.HandledError),
            k_MockHelper.MockLogger.Object,
            m_Context!));
        TestsHelper.VerifyLoggerWasCalled(
            k_MockHelper.MockLogger,
            LogLevel.Error,
            null,
            Times.Once,
            errorMessage);
        m_MockAnsiConsole.Verify(ex => ex.Write(It.IsAny<IRenderable>()), Times.Never);
        Assert.AreEqual(expectedExitCode, m_Context!.ExitCode);
    }

    [Test]
    public void HandleUnhandledCliException()
    {
        var expectedExitCode = ExitCode.UnhandledError;
        var exception = new Exception();
        Assert.DoesNotThrow(() => m_ExceptionHelper!.HandleException(
            exception,
            k_MockHelper.MockLogger.Object,
            m_Context!));
        TestsHelper.VerifyLoggerWasCalled(
            k_MockHelper.MockLogger,
            LogLevel.Error,
            null,
            Times.Never);
        m_MockAnsiConsole.Verify(ex => ex.Write(It.IsAny<IRenderable>()), Times.Once);
        Assert.AreEqual(expectedExitCode, m_Context!.ExitCode);
    }

    [Test]
    public void HandleAggregateExceptionWithOnlyHandledExceptions()
    {
        var expectedExitCode = ExitCode.HandledError;
        var exception = new CliException(ExitCode.HandledError);

        var exceptions = new List<Exception>
        {
            exception,
            exception
        };

        Assert.DoesNotThrow(() => m_ExceptionHelper!.HandleException(
            new AggregateException(exceptions),
            k_MockHelper.MockLogger.Object,
            m_Context!));

        k_MockHelper.MockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception?, string>>((o, t) => true)),
            Times.Exactly(exceptions.Count));

        m_MockAnsiConsole.Verify(ex => ex.Write(It.IsAny<IRenderable>()), Times.Never);
        Assert.AreEqual(expectedExitCode, m_Context!.ExitCode);
    }

    [Test]
    public void HandleAggregateExceptionWithHandledAndUnhandledExceptions()
    {
        var expectedExitCode = ExitCode.UnhandledError;

        var exceptions = new List<Exception>
        {
            new CliException(ExitCode.HandledError),
            new(),
            new()
        };

        Assert.DoesNotThrow(() => m_ExceptionHelper!.HandleException(
            new AggregateException(exceptions),
            k_MockHelper.MockLogger.Object,
            m_Context!));

        k_MockHelper.MockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception?, string>>((o, t) => true)),
            Times.Exactly(1));

        m_MockAnsiConsole.Verify(ex => ex.Write(It.IsAny<IRenderable>()), Times.Once);
        Assert.AreEqual(expectedExitCode, m_Context!.ExitCode);
    }

    [Test]
    public void HandleUnhandledException()
    {
        var expectedExitCode = ExitCode.UnhandledError;
        Assert.DoesNotThrow(() => m_ExceptionHelper!.HandleException(
            new CookieException(),
            k_MockHelper.MockLogger.Object,
            m_Context!));
        m_MockAnsiConsole.Verify(ex => ex.Write(It.IsAny<IRenderable>()));
        Assert.AreEqual(expectedExitCode, m_Context!.ExitCode);
    }

    [Test]
    public void HandleDeploymentFailureException()
    {
        var expectedExitCode = ExitCode.HandledError;
        Assert.DoesNotThrow(() => m_ExceptionHelper!.HandleException(
            new DeploymentFailureException(),
            k_MockHelper.MockLogger.Object,
            m_Context!));
        m_MockAnsiConsole.Verify(ex => ex.Write(It.IsAny<IRenderable>()), Times.Never);
        Assert.AreEqual(expectedExitCode, m_Context!.ExitCode);
    }

    [Test]
    public void HandleForbidden403Exception()
    {
        var identityApiException = new IdentityApiException(Convert.ToInt32(HttpStatusCode.Forbidden), null);
        Assert.DoesNotThrow(() => m_ExceptionHelper!.HandleException(
            identityApiException,
            k_MockHelper.MockLogger.Object,
            m_Context!));
        TestsHelper.VerifyLoggerWasCalled(
            k_MockHelper.MockLogger,
            LogLevel.Error,
            null,
            Times.Once,
            $"{ExceptionHelper.TroubleshootingHelp}{System.Environment.NewLine}" +
            $"{m_ExceptionHelper!.httpErrorTroubleshootingLinks[HttpStatusCode.Forbidden]}");
    }

    [Test]
    public void ExceptionHandler_DoesNotThrowWhenDiagnosticsFailToSend()
    {
        var exception = new Exception("");

        k_MockHelper.MockDiagnostics.Setup(ex => ex
                .Send())
            .Throws(new Exception());

        Assert.DoesNotThrow(() => m_ExceptionHelper!.HandleException(
            exception,
            k_MockHelper.MockLogger.Object,
            m_Context!));
    }

    [Test]
    public void ExceptionHandler_ExecuteUnhandledExceptionFlowCorrectly()
    {
        var exception = new CookieException();
        Assert.DoesNotThrow(() => m_ExceptionHelper!.HandleException(
            exception,
            k_MockHelper.MockLogger.Object,
            m_Context!));

        k_MockHelper.MockDiagnostics.Verify(
            ex =>
                ex.AddData(DiagnosticsTagKeys.DiagnosticName, "cli_unhandled_exception"),
            Times.Once);
        k_MockHelper.MockDiagnostics.Verify(
            ex =>
                ex.AddData(DiagnosticsTagKeys.DiagnosticMessage, exception.ToString()),
            Times.Once);

        var command = new StringBuilder(m_Context!.ParseResult.CommandResult.Command.Name);
        foreach (var arg in m_Context!.ParseResult.Tokens)
        {
            command.Append("_" + arg);
        }

        k_MockHelper.MockDiagnostics.Verify(
            ex =>
                ex.AddData(DiagnosticsTagKeys.Command, command.ToString()),
            Times.Once);

        k_MockHelper.MockDiagnostics.Verify(
            ex =>
                ex.AddData(TagKeys.Timestamp, It.IsAny<long>()),
            Times.Once);
    }

    [Test]
    public void ParseApiException_WithNullOrEmptyMessage_ReturnsFallback()
    {
        var exception = new Exception("");
        const string troubleShootingLink = "https://example.com/help";

        var result = ExceptionHelper.ParseApiException(exception, troubleShootingLink);

        var expectedFallback = string.Join(
            System.Environment.NewLine,
            exception.Message,
            ExceptionHelper.TroubleshootingHelp,
            troubleShootingLink);
        Assert.AreEqual(expectedFallback, result);
    }

    [Test]
    public void ParseApiException_WithNullMessage_ReturnsFallbackWithoutTroubleshooting()
    {
        var exception = new Exception("");

        var result = ExceptionHelper.ParseApiException(exception, null);

        Assert.AreEqual(exception.Message, result);
    }

    [Test]
    public void ParseApiException_WithWhitespaceMessage_ReturnsFallback()
    {
        var exception = new Exception("   \n\t   ");
        const string troubleShootingLink = "https://example.com/help";

        var result = ExceptionHelper.ParseApiException(exception, troubleShootingLink);

        var expectedFallback = string.Join(
            System.Environment.NewLine,
            exception.Message,
            ExceptionHelper.TroubleshootingHelp,
            troubleShootingLink);
        Assert.AreEqual(expectedFallback, result);
    }

    [Test]
    public void ParseApiException_WithNonMatchingPattern_ReturnsFallback()
    {
        var exception = new Exception("Some random error message");
        const string troubleShootingLink = "https://example.com/help";

        var result = ExceptionHelper.ParseApiException(exception, troubleShootingLink);

        var expectedFallback = string.Join(
            System.Environment.NewLine,
            exception.Message,
            ExceptionHelper.TroubleshootingHelp,
            troubleShootingLink);
        Assert.AreEqual(expectedFallback, result);
    }

    [Test]
    public void ParseApiException_WithMatchingPatternAndNoTroubleshootingLink_ReturnsParsedMessage()
    {
        const string jsonContent = """{"error":"Authentication failed"}""";
        var exception = new Exception($"Error calling GetUser: {jsonContent}");

        var result = ExceptionHelper.ParseApiException(exception, null);

        StringAssert.Contains("""Error calling": "GetUser""", result);
        StringAssert.Contains("""error": "Authentication failed""", result);
    }

    [Test]
    public void ParseApiException_WithMatchingPatternAndValidJson_ReturnsJsonWithTroubleshootingLink()
    {
        const string jsonContent = """{"error":"Authentication failed"}""";
        var exception = new Exception($"Error calling GetUser: {jsonContent}");
        const string troubleShootingLink = "https://example.com/help";

        var result = ExceptionHelper.ParseApiException(exception, troubleShootingLink);

        Assert.That(result, Does.Contain(@"""error"": ""Authentication failed"""));
        Assert.That(result, Does.Contain($@"""{ExceptionHelper.TroubleshootingHelp}"": ""{troubleShootingLink}"""));
    }

    [Test]
    public void ParseApiException_WithMatchingPatternButInvalidJson_ReturnsFallback()
    {
        const string invalidJson = """{"error":"invalid json""";
        var exception = new Exception($"Error calling GetUser: {invalidJson}");
        const string troubleShootingLink = "https://example.com/help";

        var result = ExceptionHelper.ParseApiException(exception, troubleShootingLink);

        var expectedFallback = string.Join(
            System.Environment.NewLine,
            exception.Message,
            ExceptionHelper.TroubleshootingHelp,
            troubleShootingLink);
        Assert.AreEqual(expectedFallback, result);
    }

    [Test]
    public void ParseApiException_WithComplexJson_PreservesStructure()
    {
        const string jsonContent =
            """{"errors":[{"code":"INVALID_REQUEST","message":"Field is required"}],"status":400}""";
        var exception = new Exception($"Error calling UpdateData: {jsonContent}");
        const string troubleShootingLink = "https://example.com/help";

        var result = ExceptionHelper.ParseApiException(exception, troubleShootingLink);

        Assert.That(result, Does.Contain(@"""errors"""));
        Assert.That(result, Does.Contain(@"""INVALID_REQUEST"""));
        Assert.That(result, Does.Contain(@"""status"": 400"));
        Assert.That(result, Does.Contain($@"""{ExceptionHelper.TroubleshootingHelp}"": ""{troubleShootingLink}"""));
    }

    [Test]
    public void ParseApiException_WithDifferentApiCallPatterns_ExtractsCorrectly()
    {
        var testCases = new[]
        {
            ("Error calling CreateUser: {\"error\":\"exists\"}", "CreateUser"),
            ("Error calling DeleteFile: {\"message\":\"not found\"}", "DeleteFile"),
            ("Error calling ValidateToken: {\"expired\":true}", "ValidateToken")
        };

        foreach (var (message, expectedApiCall) in testCases)
        {
            var exception = new Exception(message);
            var result = ExceptionHelper.ParseApiException(exception, null);

            Assert.That(result, Does.Not.Contain($"Error calling {expectedApiCall}:"));
            Assert.That(result, Does.StartWith("{"));
        }
    }
}
