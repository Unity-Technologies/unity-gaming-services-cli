using Moq;
using NUnit.Framework;
using Unity.Services.Cli.RemoteConfig.Service;
using Unity.Services.Cli.Common.Exceptions;
using Unity.Services.Cli.Common.Models;
using Unity.Services.Cli.Common.Validator;
using Unity.Services.Cli.RemoteConfig.Types;
using Unity.Services.Cli.ServiceAccountAuthentication;

namespace Unity.Services.Cli.RemoteConfig.UnitTest.Service;

[TestFixture]
class RemoteConfigServiceTests
{
    const string k_TestProjectId = "a912b1fd-541d-42e1-89f2-85436f27aabd";
    const string k_TestEnvironmentId = "test-env-id";
    const string k_TestAccessToken = "test-token";
    const string k_InvalidProjectId = "invalidProject";
    const string k_ConfigId = "97dfdc30-9bb8-46f2-89c5-7df39232e686";
    const string k_EmptyConfigId = "";
    const string k_ConfigBody = @"{""type"": ""settings"",""value"": [{""key"": ""testConfig"", ""type"": ""int"", ""value"": 1}]}";

    Mock<IConfigurationValidator> m_ValidatorObject = new();
    Mock<IServiceAccountAuthenticationService> m_AuthenticationServiceObject = new();
    RemoteConfigService? m_RemoteConfigService;

    [SetUp]
    public void SetUp()
    {
        m_AuthenticationServiceObject.Reset();

        m_ValidatorObject = new Mock<IConfigurationValidator>();
        m_AuthenticationServiceObject = new Mock<IServiceAccountAuthenticationService>();
        m_AuthenticationServiceObject.Setup(a => a.GetAccessTokenAsync(CancellationToken.None))
            .Returns(Task.FromResult(k_TestAccessToken));

        m_RemoteConfigService = new RemoteConfigService(
            m_AuthenticationServiceObject.Object,
            m_ValidatorObject.Object);
    }

    [Test]
    public void GetAllConfigsFromEnvironmentAsync_InvalidProjectIDThrowConfigValidationException()
    {
        m_ValidatorObject.Setup(v => v.ThrowExceptionIfConfigInvalid(Keys.ConfigKeys.ProjectId, k_InvalidProjectId))
            .Throws(new ConfigValidationException(Keys.ConfigKeys.ProjectId, k_InvalidProjectId, It.IsAny<string>()));
        Assert.ThrowsAsync<ConfigValidationException>(() =>
            m_RemoteConfigService!.GetAllConfigsFromEnvironmentAsync(k_InvalidProjectId, k_TestEnvironmentId, null, CancellationToken.None));
    }

    [Test]
    public void GetAllConfigsFromEnvironmentAsync_CallsAuthorizeServiceAsync()
    {
        m_ValidatorObject.Setup(v => v.ThrowExceptionIfConfigInvalid(Keys.ConfigKeys.ProjectId, k_InvalidProjectId))
            .Throws(new ConfigValidationException(Keys.ConfigKeys.ProjectId, k_InvalidProjectId, It.IsAny<string>()));
        m_RemoteConfigService!.GetAllConfigsFromEnvironmentAsync(k_TestProjectId, k_TestEnvironmentId, null, CancellationToken.None);
        m_AuthenticationServiceObject.Verify(x => x.GetAccessTokenAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public void UpdateConfigAsync_InvalidProjectIDThrowConfigValidationException()
    {
        m_ValidatorObject.Setup(v => v.ThrowExceptionIfConfigInvalid(Keys.ConfigKeys.ProjectId, k_InvalidProjectId))
            .Throws(new ConfigValidationException(Keys.ConfigKeys.ProjectId, k_InvalidProjectId, It.IsAny<string>()));
        Assert.ThrowsAsync<ConfigValidationException>(() =>
            m_RemoteConfigService!.UpdateConfigAsync(k_InvalidProjectId, k_ConfigId, k_ConfigBody, CancellationToken.None));
    }

    [Test]
    public void UpdateConfigAsync_InvalidConfigIDThrowConfigValidationException()
    {
        Assert.ThrowsAsync<ArgumentException>(() =>
            m_RemoteConfigService!.UpdateConfigAsync(k_TestProjectId, k_EmptyConfigId, k_ConfigBody, CancellationToken.None));
    }

    [Test]
    public void UpdateConfigAsync_CallsAuthorizeServiceAsync()
    {
        m_ValidatorObject.Setup(v => v.ThrowExceptionIfConfigInvalid(Keys.ConfigKeys.ProjectId, k_InvalidProjectId));
        m_RemoteConfigService!.UpdateConfigAsync(k_TestProjectId, "configId", k_ConfigBody, CancellationToken.None);
        m_AuthenticationServiceObject.Verify(x => x.GetAccessTokenAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public void CreateConfigAsync_InvalidProjectIDThrowConfigValidationException()
    {
        m_ValidatorObject.Setup(v => v.ThrowExceptionIfConfigInvalid(Keys.ConfigKeys.ProjectId, k_InvalidProjectId))
            .Throws(new ConfigValidationException(Keys.ConfigKeys.ProjectId, k_InvalidProjectId, It.IsAny<string>()));
        Assert.ThrowsAsync<ConfigValidationException>(() =>
            m_RemoteConfigService!.CreateConfigAsync(k_InvalidProjectId, k_TestEnvironmentId, "settings", new List<ConfigValue>(), CancellationToken.None));
    }

    [Test]
    public void CreateConfigAsync_CallsAuthentication()
    {
        m_ValidatorObject.Setup(v => v.ThrowExceptionIfConfigInvalid(Keys.ConfigKeys.ProjectId, k_InvalidProjectId));
        m_RemoteConfigService!.CreateConfigAsync(k_TestProjectId, k_TestEnvironmentId, "settings", new List<ConfigValue>(), CancellationToken.None);
        m_AuthenticationServiceObject.Verify(x => x.GetAccessTokenAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
