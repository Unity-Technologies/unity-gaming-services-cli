using Moq;
using NUnit.Framework;
using Unity.Services.Cli.RemoteConfig.Service;
using Unity.Services.Cli.Common.Exceptions;
using Unity.Services.Cli.Common.Models;
using Unity.Services.Cli.Common.Validator;
using Unity.Services.Cli.RemoteConfig.Types;
using Unity.Services.Cli.ServiceAccountAuthentication;
using Unity.Services.Cli.Common.Networking;
using System.Reflection;
using Unity.Services.Cli.RemoteConfig.Exceptions;

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
    const string k_ConfigBody = @"{""type"": ""alt"",""value"": [{""key"": ""testConfig"", ""type"": ""int"", ""value"": 1}]}";

    Mock<IConfigurationValidator> m_ValidatorObject = new();
    Mock<IServiceAccountAuthenticationService> m_AuthenticationServiceObject = new();
    RemoteConfigService? m_RemoteConfigService;

    [SetUp]
    public void SetUp()
    {
        EndpointHelper.InitializeNetworkTargetEndpoints(new[]
        {
            typeof(RemoteConfigEndpoints).GetTypeInfo(),
            typeof(RemoteConfigInternalEndpoints).GetTypeInfo()
        });

        m_AuthenticationServiceObject.Reset();

        m_ValidatorObject = new Mock<IConfigurationValidator>();
        m_ValidatorObject.Setup(v => v.ThrowExceptionIfConfigInvalid(Keys.ConfigKeys.ProjectId, k_InvalidProjectId))
            .Throws(new ConfigValidationException(Keys.ConfigKeys.ProjectId, k_InvalidProjectId, It.IsAny<string>()));
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
        Assert.ThrowsAsync<ConfigValidationException>(() =>
            m_RemoteConfigService!.GetAllConfigsFromEnvironmentAsync(k_InvalidProjectId, k_TestEnvironmentId, null, CancellationToken.None));
    }

    [Test]
    public async Task GetAllConfigsFromEnvironmentAsync_CallsAuthorizeServiceAsync()
    {
        try
        {
            await m_RemoteConfigService!.GetAllConfigsFromEnvironmentAsync(k_TestProjectId, k_TestEnvironmentId, null, CancellationToken.None);
        }
        catch (ApiException)
        {
            // These tests are not intending to test the actual APIs so these calls will fail because we don't
            // have a valid token. We just want to make sure that the class is getting the token.
        }
        m_AuthenticationServiceObject.Verify(x => x.GetAccessTokenAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public void UpdateConfigAsync_InvalidProjectIDThrowConfigValidationException()
    {
        Assert.ThrowsAsync<ConfigValidationException>(() =>
            m_RemoteConfigService!.UpdateConfigAsync(k_InvalidProjectId, k_ConfigId, k_ConfigBody, CancellationToken.None));
    }

    [Test]
    public void UpdateConfigAsync_InvalidConfigIDThrowArgumentException()
    {
        Assert.ThrowsAsync<ArgumentException>(() =>
            m_RemoteConfigService!.UpdateConfigAsync(k_TestProjectId, k_EmptyConfigId, k_ConfigBody, CancellationToken.None));
    }

    [Test]
    public void UpdateConfigAsync_InvalidBodyThrowCliExceptionAsync()
    {
        Assert.ThrowsAsync<CliException>(() =>
            m_RemoteConfigService!.UpdateConfigAsync(k_TestProjectId, "configId", "><> I'm a FISH! ><>", CancellationToken.None),
            "Config request body contains invalid JSON");
    }

    [Test]
    public void UpdateConfigAsync_NullJsonBodyThrowCliExceptionAsync()
    {
        Assert.ThrowsAsync<CliException>(() =>
            m_RemoteConfigService!.UpdateConfigAsync(k_TestProjectId, "configId", "null", CancellationToken.None),
            "Empty config request body");
    }

    [Test]
    public void UpdateConfigAsync_BodyMissingTypeThrowCliExceptionAsync()
    {
        Assert.ThrowsAsync<CliException>(() =>
            m_RemoteConfigService!.UpdateConfigAsync(k_TestProjectId, "configId", "{}", CancellationToken.None),
            "Config request body is missing type");
    }

    [Test]
    public async Task UpdateConfigAsync_CallsAuthorizeServiceAsync()
    {
        try
        {
            await m_RemoteConfigService!.UpdateConfigAsync(k_TestProjectId, "configId", k_ConfigBody, CancellationToken.None);
        }
        catch (ApiException)
        {
            // These tests are not intending to test the actual APIs so these calls will fail because we don't
            // have a valid token. We just want to make sure that the class is getting the token.
        }
        m_AuthenticationServiceObject.Verify(x => x.GetAccessTokenAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public void CreateConfigAsync_InvalidProjectIDThrowConfigValidationException()
    {
        Assert.ThrowsAsync<ConfigValidationException>(() =>
            m_RemoteConfigService!.CreateConfigAsync(k_InvalidProjectId, k_TestEnvironmentId, "settings", new List<ConfigValue>(), CancellationToken.None));
    }

    [Test]
    public async Task CreateConfigAsync_CallsAuthorizeServiceAsync()
    {
        try
        {
            await m_RemoteConfigService!.CreateConfigAsync(k_TestProjectId, k_TestEnvironmentId, "alt", new List<ConfigValue>(), CancellationToken.None);
        }
        catch (ApiException)
        {
            // These tests are not intending to test the actual APIs so these calls will fail because we don't
            // have a valid token. We just want to make sure that the class is getting the token.
        }
        m_AuthenticationServiceObject.Verify(x => x.GetAccessTokenAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public void DeleteConfigAsync_InvalidProjectIDThrowConfigValidationException()
    {
        Assert.ThrowsAsync<ConfigValidationException>(() =>
            m_RemoteConfigService!.DeleteConfigAsync(k_InvalidProjectId, k_ConfigId, "settings", CancellationToken.None));
    }

    [Test]
    public void DeleteConfigAsync_InvalidConfigIDThrowArgumentException()
    {
        Assert.ThrowsAsync<ArgumentException>(() =>
            m_RemoteConfigService!.DeleteConfigAsync(k_TestProjectId, k_EmptyConfigId, "settings", CancellationToken.None));
    }

    [Test]
    public async Task DeleteConfigAsync_CallsAuthorizeServiceAsync()
    {
        try
        {
            await m_RemoteConfigService!.DeleteConfigAsync(k_TestProjectId, k_TestEnvironmentId, "alt", CancellationToken.None);
        }
        catch (ApiException)
        {
            // These tests are not intending to test the actual APIs so these calls will fail because we don't
            // have a valid token. We just want to make sure that the class is getting the token.
        }
        m_AuthenticationServiceObject.Verify(x => x.GetAccessTokenAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public void ApplySchemaAsync_InvalidProjectIDThrowConfigValidationException()
    {
        Assert.ThrowsAsync<ConfigValidationException>(() =>
            m_RemoteConfigService!.ApplySchemaAsync(k_InvalidProjectId, k_ConfigId, k_ConfigBody, CancellationToken.None));
    }

    [Test]
    public void ApplySchemaAsync_InvalidConfigIDThrowArgumentException()
    {
        Assert.ThrowsAsync<ArgumentException>(() =>
            m_RemoteConfigService!.ApplySchemaAsync(k_TestProjectId, k_EmptyConfigId, k_ConfigBody, CancellationToken.None));
    }

    [Test]
    public async Task ApplySchemaAsync_CallsAuthorizeServiceAsync()
    {
        try
        {
            await m_RemoteConfigService!.ApplySchemaAsync(k_TestProjectId, "configId", k_ConfigBody, CancellationToken.None);
        }
        catch (ApiException)
        {
            // These tests are not intending to test the actual APIs so these calls will fail because we don't
            // have a valid token. We just want to make sure that the class is getting the token.
        }
        m_AuthenticationServiceObject.Verify(x => x.GetAccessTokenAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
