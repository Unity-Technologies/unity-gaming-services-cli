using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using System.Reflection;
using Moq;
using NUnit.Framework;
using Unity.Services.Cli.Common.Models;
using Unity.Services.Cli.Common.Networking;
using Unity.Services.Cli.Common.SystemEnvironment;
using Unity.Services.Cli.Common.Telemetry;
using Unity.Services.TelemetryApi.Generated.Api;
using Unity.Services.TelemetryApi.Generated.Model;

namespace Unity.Services.Cli.Common.UnitTest.Telemetry;

[TestFixture]
public class DiagnosticsTests
{
    readonly Mock<ITelemetryApi> m_MockTelemetryApi = new();

    readonly Dictionary<string, string> m_ExpectedCommonTags = new()
    {
        [TagKeys.OperatingSystem] = System.Environment.OSVersion.ToString(),
        [TagKeys.Platform] = TelemetryConfigurationProvider.GetOsPlatform()
    };

    readonly Dictionary<string, string> m_ExpectedPackageTags = new()
    {
        [TagKeys.ProductName] = CommonModule.m_CliProductName,
        [TagKeys.CliVersion] = TelemetryConfigurationProvider.GetCliVersion()
    };

    const string k_TestCommand = "test-module test-cmd";

    readonly Mock<ISystemEnvironmentProvider> m_MockEnvProvider = new();

    Mock<TelemetrySender> m_MockTelemetrySender;

    public DiagnosticsTests()
    {
        var types = new List<TypeInfo>
        {
            typeof(TelemetryApiEndpoints).GetTypeInfo(),
        };
        EndpointHelper.InitializeNetworkTargetEndpoints(types);

        m_MockTelemetrySender = new(m_MockTelemetryApi.Object, m_ExpectedCommonTags, m_ExpectedPackageTags);

        string errorMsg;

        m_MockEnvProvider.Setup(ex => ex.
            GetSystemEnvironmentVariable(It.IsAny<string>(), out errorMsg)).Returns("");
    }

    [SetUp]
    public void SetUp()
    {
        m_MockTelemetryApi.Reset();
        m_MockEnvProvider.Reset();
        m_MockTelemetrySender.Reset();
    }

    static object[] s_DiagnosticsMessageCases =
    {
        new object[]
        {
            "test_msg", "test_msg"
        },
        new object[]
        {
            new string('a', 10001),
            $"{new string('a', 10001).Substring(0, 10000)}" +
            $"{System.Environment.NewLine}{"[truncated]"}"
        }
    };

    [TestCaseSource(nameof(s_DiagnosticsMessageCases))]
    public void SendDiagnostic(string msg, string expectedMsg)
    {
        var name = "test_name";

        var diag = new Diagnostics(m_MockTelemetrySender.Object, m_MockEnvProvider.Object);
        diag.SendDiagnostic(name, msg, GetFakeContext());

        var postRecordRequest = CreateExpectedPostRecordRequest(name, expectedMsg);
        m_MockTelemetryApi.Verify(api => api.PostRecordWithHttpInfo(postRecordRequest, 0));
    }

    [TestCase(Keys.EnvironmentKeys.RunningOnJenkins)]
    [TestCase(Keys.EnvironmentKeys.RunningOnDocker)]
    [TestCase(Keys.EnvironmentKeys.RunningOnGithubActions)]
    [TestCase(Keys.EnvironmentKeys.RunningOnUnityCloudBuild)]
    [TestCase(Keys.EnvironmentKeys.RunningOnYamato)]
    public void SendDiagnosticAddsCorrectCicdPlatform(string platformVariable)
    {
        string errorMsg;
        m_MockEnvProvider.Setup(ex => ex.
            GetSystemEnvironmentVariable(platformVariable, out errorMsg)).Returns("yes");

        var name = "test_name";
        var msg = "test_msg";
        var expectedMsg = "test_msg";
        var diag = new Diagnostics(m_MockTelemetrySender.Object, m_MockEnvProvider.Object);
        m_MockTelemetrySender.Object.CommonTags[TagKeys.CicdPlatform] = Keys.CicdEnvVarToDisplayNamePair[platformVariable];
        diag.SendDiagnostic(name, msg, GetFakeContext());

        var postRecordRequest = CreateExpectedPostRecordRequest(name, expectedMsg);
        m_MockTelemetryApi.Verify(api => api.PostRecordWithHttpInfo(postRecordRequest, 0));
    }

    [TestCase("true")]
    [TestCase("True")]
    [TestCase("TRUE")]
    [TestCase("1")]
    public void EnvVariablePreventsFromSendingDiagnostic(string value)
    {
        // Sets env variable to disable telemetry
        string errorMsg;
        m_MockEnvProvider.Setup(ex => ex.
            GetSystemEnvironmentVariable(Keys.EnvironmentKeys.TelemetryDisabled, out errorMsg)).Returns("yes");

        var name = "name";
        var message = "message";

        // Create and Send mocked diagnostics
        var diag = new Diagnostics(m_MockTelemetrySender.Object, m_MockEnvProvider.Object);
        diag.SendDiagnostic(name, message, GetFakeContext());

        var postRecordRequest = CreateExpectedPostRecordRequest(name, message);
        m_MockTelemetryApi.Verify(api => api.PostRecordWithHttpInfo(postRecordRequest, 0), Times.Never);
    }

    static InvocationContext GetFakeContext()
    {
        var parser = new Parser(new RootCommand("Test root command"));
        var parseResult = parser.Parse(k_TestCommand);
        return new InvocationContext(parseResult);
    }

    PostRecordRequest CreateExpectedPostRecordRequest(string name, string msg)
    {
        // Prepare diagnostics list
        var diagnosticList = new List<DiagnosticEvent>();
        var diagnostic = new DiagnosticEvent
        {
            Content = new Dictionary<string, string>(m_MockTelemetrySender.Object.ProductTags)
        };
        // message = expectedMsg;
        diagnostic.Content.Add(TagKeys.DiagnosticName, name);
        diagnostic.Content.Add(TagKeys.DiagnosticMessage, msg);
        diagnostic.Content.Add(TagKeys.Command, "ugs " + k_TestCommand);
        diagnosticList.Add(diagnostic);

        // Create PostRecordRequest
        return new PostRecordRequest(
            m_MockTelemetrySender.Object.CommonTags,
            null,
            null,
            diagnosticList
        );
    }
}
