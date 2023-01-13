using Moq;
using NUnit.Framework;
using Unity.Services.Cli.Common.Exceptions;
using Unity.Services.Cli.Common.Models;
using Unity.Services.Cli.Common.Persister;
using Unity.Services.Cli.Common.Validator;

namespace Unity.Services.Cli.Common.UnitTest;

[TestFixture]
class ConfigurationServiceTests
{
    Models.Configuration m_PrefObj = new();
    Mock<IPersister<Models.Configuration>> m_MockPreferences = new();
    Mock<IConfigurationValidator> m_MockPreferencesValidator = new();
    ConfigurationService? m_ConfigServices;

    [SetUp]
    public void SetUp()
    {
        string mockErrorMsg;
        m_MockPreferencesValidator.Setup(s => s.IsConfigValid(It.IsAny<string>(), It.IsAny<string>(), out mockErrorMsg))
            .Returns(true);
        m_MockPreferencesValidator.Setup(s => s.IsKeyValid(It.IsAny<string>(), out mockErrorMsg))
            .Returns(true);
        m_MockPreferences.Setup(s => s.LoadAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(m_PrefObj)!);
        m_ConfigServices = new ConfigurationService(m_MockPreferences.Object, m_MockPreferencesValidator.Object);
    }

    [Test]
    public void SetConfigArgumentsAsync_EnvironmentTest()
    {
        string key = Models.Keys.ConfigKeys.EnvironmentName;
        string value = "stg";
        Assert.DoesNotThrowAsync(() => m_ConfigServices!.SetConfigArgumentsAsync(key, value));
        Assert.AreEqual(m_PrefObj!.EnvironmentName, value);
    }

    [Test]
    public void SetConfigArgumentsAsync_CloudProjectIdTest()
    {
        string key = Models.Keys.ConfigKeys.ProjectId;
        string value = "testproj";
        Assert.DoesNotThrowAsync(() => m_ConfigServices!.SetConfigArgumentsAsync(key, value));
        Assert.AreEqual(m_PrefObj!.CloudProjectId, value);
    }

    [Test]
    public void SetConfigArgumentsAsync_InvalidValueTest()
    {
        string mockErrorMsg;
        m_MockPreferencesValidator!.Setup(s => s.IsConfigValid(It.IsAny<string>(), It.IsAny<string>(), out mockErrorMsg))
            .Returns(false);
        m_ConfigServices = new ConfigurationService(m_MockPreferences!.Object, m_MockPreferencesValidator.Object);

        Assert.ThrowsAsync<ConfigValidationException>(() =>
            m_ConfigServices.SetConfigArgumentsAsync(Models.Keys.ConfigKeys.ProjectId, "testValue"));
    }

    [TestCase(Models.Keys.ConfigKeys.EnvironmentName, "prod")]
    [TestCase(Models.Keys.ConfigKeys.ProjectId, "testproj")]
    public async Task GetConfigArgumentsAsyncTest(string key, string value)
    {
        m_PrefObj!.SetValue(key, value);
        var configArguments = await m_ConfigServices!.GetConfigArgumentsAsync(key);
        Assert.AreEqual(value, configArguments);
    }

    [Test]
    public Task GetConfigArgumentsAsyncThrowsMissingException()
    {
        m_PrefObj!.SetValue("project-id", "");
        Assert.ThrowsAsync<MissingConfigurationException>(() => m_ConfigServices!.GetConfigArgumentsAsync("project-id"));
        return Task.CompletedTask;
    }

    [TestCase(null)]
    [TestCase("")]
    public void GetConfigArgumentThrowsExceptionOnNullConfigKey(string key)
    {
        string mockErrorMsg;
        m_MockPreferencesValidator.Setup(s => s.IsKeyValid(It.IsAny<string>(), out mockErrorMsg))
            .Returns(false);
        Assert.ThrowsAsync<ConfigValidationException>(() => m_ConfigServices!.GetConfigArgumentsAsync(key));
    }

    [Test]
    public void GetConfigArgumentThrowsExceptionOnInvalidConfigKey()
    {
        string mockErrorMsg;
        m_MockPreferencesValidator.Setup(s => s.IsKeyValid(It.IsAny<string>(), out mockErrorMsg))
            .Returns(false);
        Assert.ThrowsAsync<ConfigValidationException>(() => m_ConfigServices!.GetConfigArgumentsAsync("abc"));
    }

    [Test]
    public void GetConfigArgumentThrowsMissingConfigurationException()
    {
        Assert.ThrowsAsync<MissingConfigurationException>(() => m_ConfigServices!.GetConfigArgumentsAsync("project-id"));
    }

    [Test]
    public void DeleteConfigArgumentsAsyncThrowsWhenKeyInvalid()
    {
        string mockErrorMsg;
        m_MockPreferencesValidator.Setup(s => s.IsKeyValid(It.IsAny<string>(), out mockErrorMsg))
            .Returns(false);
        Assert.ThrowsAsync<ConfigValidationException>(() => m_ConfigServices!
            .DeleteConfigArgumentsAsync(new []{"invalid-key"}));
    }

    [Test]
    public async Task DeleteConfigArgumentsAsyncDeletesKeyValue()
    {
        m_PrefObj.SetValue(Keys.ConfigKeys.EnvironmentName, "value");
        await m_ConfigServices!.DeleteConfigArgumentsAsync(new []{Keys.ConfigKeys.EnvironmentName});
        var value = m_PrefObj.GetValue(Keys.ConfigKeys.EnvironmentName);
        Assert.IsNull(value);
    }
}
