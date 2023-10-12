using Moq;
using NUnit.Framework;
using Unity.Services.Cli.Common.Exceptions;
using Unity.Services.Cli.Common.Models;
using Unity.Services.Cli.Common.Validator;
using Unity.Services.Cli.ServiceAccountAuthentication;
using Unity.Services.Cli.ServiceAccountAuthentication.Token;
using Unity.Services.Cli.Triggers.Service;
using Unity.Services.Gateway.TriggersApiV1.Generated.Api;
using Unity.Services.Gateway.TriggersApiV1.Generated.Model;

namespace Unity.Services.Cli.Triggers.UnitTest.Service;

[TestFixture]
class TriggersServiceTests
{
    const string k_TestAccessToken = "test-token";

    const string k_ValidProjectId = "a912b1fd-541d-42e1-89f2-85436f27aabd";
    const string k_ValidEnvironmentId = "00000000-0000-0000-0000-000000000000";
    const string k_InvalidProjectId = "invalidProject";
    const string k_InvalidEnvironmentId = "foo";
    const string k_TriggerId = "42424242-4242-4242-4242-424242424242";

    readonly Mock<IConfigurationValidator> m_ValidatorObject = new();
    readonly Mock<IServiceAccountAuthenticationService> m_AuthenticationServiceObject = new();
    readonly Mock<ITriggersApiAsync> m_TriggersApiMock = new();

    TriggersService? m_TriggersService;

    [SetUp]
    public void SetUp()
    {
        m_ValidatorObject.Reset();
        m_AuthenticationServiceObject.Reset();
        m_AuthenticationServiceObject.Setup(a => a.GetAccessTokenAsync(CancellationToken.None))
            .Returns(Task.FromResult(k_TestAccessToken));

        m_TriggersApiMock.Reset();
        m_TriggersApiMock.Setup(a => a.Configuration)
            .Returns(new Gateway.TriggersApiV1.Generated.Client.Configuration());

        m_TriggersService = new TriggersService(
            m_TriggersApiMock.Object,
            m_ValidatorObject.Object,
            m_AuthenticationServiceObject.Object);
    }

    [Test]
    public async Task AuthorizeTriggerService()
    {
        await m_TriggersService!.AuthorizeServiceAsync(CancellationToken.None);
        m_AuthenticationServiceObject.Verify(a => a.GetAccessTokenAsync(CancellationToken.None));
        Assert.AreEqual(
            k_TestAccessToken.ToHeaderValue(),
            m_TriggersApiMock.Object.Configuration.DefaultHeaders[
                AccessTokenHelper.HeaderKey]);
    }

    [Test]
    public async Task ListAsync_Success()
    {
        string mockErrorMsg;
        m_ValidatorObject.Setup(v => v.IsConfigValid(It.IsAny<string>(), It.IsAny<string>(), out mockErrorMsg))
            .Returns(true);

        var result = new TriggerConfigPage(Guid.Empty, Guid.Empty, new List<TriggerConfig>(){new (), new()});
        m_TriggersApiMock.Setup(
            t => t.ListTriggerConfigsAsync(
                It.Is<Guid>(id => id.ToString() == k_ValidProjectId),
                It.Is<Guid>(id => id.ToString() == k_ValidEnvironmentId),
                It.IsAny<int?>(),
                It.IsAny<string?>(),
                0,
                CancellationToken.None)).ReturnsAsync(result);

        var actual = await m_TriggersService!.GetTriggersAsync(
            k_ValidProjectId, k_ValidEnvironmentId, null, CancellationToken.None);

        m_TriggersApiMock.VerifyAll();
        Assert.AreEqual(2, actual.Count());
    }

    [Test]
    public void InvalidProjectIdThrowConfigValidationException()
    {
        m_ValidatorObject.Setup(v => v.ThrowExceptionIfConfigInvalid(Keys.ConfigKeys.ProjectId, k_InvalidProjectId))
            .Throws(new ConfigValidationException(Keys.ConfigKeys.EnvironmentId, k_InvalidEnvironmentId, It.IsAny<string>()));
        Assert.Throws<ConfigValidationException>(
            () => m_TriggersService!.ValidateProjectIdAndEnvironmentId(
                k_InvalidProjectId, k_ValidEnvironmentId));
    }

    [Test]
    public void InvalidEnvironmentIdThrowConfigValidationException()
    {
        m_ValidatorObject.Setup(v => v.ThrowExceptionIfConfigInvalid(Keys.ConfigKeys.EnvironmentId, k_InvalidEnvironmentId))
            .Throws(new ConfigValidationException(Keys.ConfigKeys.EnvironmentId, k_InvalidEnvironmentId, It.IsAny<string>()));
        Assert.Throws<ConfigValidationException>(
            () => m_TriggersService!.ValidateProjectIdAndEnvironmentId(
                k_ValidProjectId, k_InvalidEnvironmentId));
    }

    [Test]
    public async Task CreateAsync_Succeeded()
    {
        string mockErrorMsg;
        m_ValidatorObject.Setup(v => v.IsConfigValid(It.IsAny<string>(), It.IsAny<string>(), out mockErrorMsg))
            .Returns(true);

        var config = new TriggerConfigBody(
            "name",
            "eventType",
            TriggerActionType.CloudCode,
            "cc/blah");
        m_TriggersApiMock.Setup(
            t => t.CreateTriggerConfigAsync(
                It.Is<Guid>(id => id.ToString() == k_ValidProjectId),
                It.Is<Guid>(id => id.ToString() == k_ValidEnvironmentId),
                config,
                0,
                CancellationToken.None
            ));

        await m_TriggersService!.CreateTriggerAsync(
            k_ValidProjectId, k_ValidEnvironmentId,
            config,
            CancellationToken.None);
    }

    [Test]
    public async Task UpdateAsync_Succeeded()
    {
        string mockErrorMsg;
        m_ValidatorObject.Setup(v => v.IsConfigValid(It.IsAny<string>(), It.IsAny<string>(), out mockErrorMsg))
            .Returns(true);
        var updatedConfig = new TriggerConfigBody("name", "eventType", TriggerActionType.CloudCode, "urn");
        m_TriggersApiMock.Setup(
            t => t.DeleteTriggerConfigAsync(
                It.Is<Guid>(id => id.ToString() == k_ValidProjectId),
                It.Is<Guid>(id => id.ToString() == k_ValidEnvironmentId),
                It.Is<Guid>(id => id.ToString() == k_TriggerId),
                0,
                CancellationToken.None
            ));
        m_TriggersApiMock.Setup(
            t => t.CreateTriggerConfigAsync(
                It.Is<Guid>(id => id.ToString() == k_ValidProjectId),
                It.Is<Guid>(id => id.ToString() == k_ValidEnvironmentId),
                updatedConfig,
                0,
                CancellationToken.None
            ));
        await m_TriggersService!.UpdateTriggerAsync(
            k_ValidProjectId,
            k_ValidEnvironmentId,
            k_TriggerId,
            updatedConfig,
            CancellationToken.None);
        m_TriggersApiMock.VerifyAll();
    }

    [Test]
    public async Task DeleteTriggerAsync()
    {
        string mockErrorMsg;
        m_ValidatorObject.Setup(v => v.IsConfigValid(It.IsAny<string>(), It.IsAny<string>(), out mockErrorMsg))
            .Returns(true);

        m_TriggersApiMock.Setup(
            t => t.DeleteTriggerConfigAsync(
                It.Is<Guid>(id => id.ToString() == k_ValidProjectId),
                It.Is<Guid>(id => id.ToString() == k_ValidEnvironmentId),
                It.Is<Guid>(id => id.ToString() == k_TriggerId),
                0,
                CancellationToken.None
            ));
        await m_TriggersService!.DeleteTriggerAsync(
            k_ValidProjectId, k_ValidEnvironmentId, k_TriggerId, CancellationToken.None);

        m_TriggersApiMock.VerifyAll();
    }
}
