using System.Net;
using Moq;
using NUnit.Framework;
using Unity.Services.Cli.Common.Validator;
using Unity.Services.Cli.Observability.Service;
using Unity.Services.Cli.Observability.UnitTest.Mock;
using Unity.Services.Cli.Observability.UnitTest.Utils;
using Unity.Services.Cli.ServiceAccountAuthentication;
using Unity.Services.Cli.ServiceAccountAuthentication.Token;
using Unity.Services.Gateway.ObservabilityApiV1.Generated.Client;
using Unity.Services.Gateway.ObservabilityApiV1.Generated.Model;

namespace Unity.Services.Cli.Observability.UnitTest.Service;

[TestFixture]
class ObservabilityServiceTests
{
    const string k_TestAccessToken = "test-token";
    const string k_From = "now-3h";
    const string k_To = "now";
    const string k_Query = "severityText = \"Error\"";

    readonly Mock<IConfigurationValidator> m_ValidatorObject = new();
    readonly Mock<IServiceAccountAuthenticationService> m_AuthenticationServiceObject = new();
    readonly LogsApiAsyncMock m_LogsApiAsyncMock = new();

    ObservabilityService? m_ObservabilityService;

    [SetUp]
    public void SetUp()
    {
        m_ValidatorObject.Reset();
        m_AuthenticationServiceObject.Reset();
        m_AuthenticationServiceObject.Setup(a => a.GetAccessTokenAsync(CancellationToken.None))
            .Returns(Task.FromResult(k_TestAccessToken));

        m_LogsApiAsyncMock.SetUp();

        m_ObservabilityService = new ObservabilityService(
            m_LogsApiAsyncMock.DefaultApiAsyncObject.Object,
            m_ValidatorObject.Object,
            m_AuthenticationServiceObject.Object);
    }

    [Test]
    public async Task AuthorizeObservabilityService()
    {
        await m_ObservabilityService!.AuthorizeServiceAsync(CancellationToken.None);
        m_AuthenticationServiceObject.Verify(a => a.GetAccessTokenAsync(CancellationToken.None));
        Assert.AreEqual(
            k_TestAccessToken.ToHeaderValue(),
            m_LogsApiAsyncMock.DefaultApiAsyncObject.Object.Configuration.DefaultHeaders[AccessTokenHelper.HeaderKey]);
    }

    [Test]
    public async Task GetLogsAsync_ValidParamsForwardsToApi()
    {
        string mockErrorMsg;
        m_ValidatorObject.Setup(v => v.IsConfigValid(It.IsAny<string>(), It.IsAny<string>(), out mockErrorMsg))
            .Returns(true);

        var response = await m_ObservabilityService!.GetLogsAsync(
            TestValues.ValidProjectId,
            TestValues.ValidEnvironmentId,
            k_From,
            k_To,
            k_Query,
            0,
            50,
            CancellationToken.None);

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        m_LogsApiAsyncMock.DefaultApiAsyncObject.Verify(
            a => a.GetLogsWithHttpInfoAsync(
                TestValues.ValidProjectId,
                Guid.Parse(TestValues.ValidEnvironmentId),
                0,
                50,
                k_From,
                k_To,
                k_Query,
                0,
                CancellationToken.None),
            Times.Once);
    }

    [Test]
    public async Task GetLogsAsync_ValidatesProjectAndEnvironment()
    {
        string mockErrorMsg;
        m_ValidatorObject.Setup(v => v.IsConfigValid(It.IsAny<string>(), It.IsAny<string>(), out mockErrorMsg))
            .Returns(true);

        await m_ObservabilityService!.GetLogsAsync(
            TestValues.ValidProjectId,
            TestValues.ValidEnvironmentId,
            null,
            null,
            null,
            null,
            null,
            CancellationToken.None);

        m_ValidatorObject.Verify(
            v => v.ThrowExceptionIfConfigInvalid(It.IsAny<string>(), It.IsAny<string>()),
            Times.Exactly(2));
    }
}
