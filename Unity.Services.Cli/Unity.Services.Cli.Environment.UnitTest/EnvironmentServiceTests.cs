using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using Unity.Services.Cli.Common.Exceptions;
using Unity.Services.Cli.Common.Models;
using Unity.Services.Cli.Common.Validator;
using Unity.Services.Cli.Environment.UnitTest.Mock;
using Unity.Services.Cli.ServiceAccountAuthentication;
using Unity.Services.Cli.ServiceAccountAuthentication.Token;
using Unity.Services.Gateway.IdentityApiV1.Generated.Model;

namespace Unity.Services.Cli.Environment.UnitTest;

[TestFixture]
class EnvironmentServiceTests
{
    const string k_TestProjectId = "a912b1fd-541d-42e1-89f2-85436f27aabd";
    const string k_TestEnvironmentName = "test";
    const string k_TestEnvironmentId = "test-env-id";
    const string k_TestAccessToken = "test-token";
    Mock<IConfigurationValidator> m_ValidatorObject = new();
    Mock<IServiceAccountAuthenticationService> m_AuthenticationServiceObject = new();

    EnvironmentService? m_EnvService;
    List<EnvironmentResponse>? m_ExpectedEnvironments;
    readonly IdentityApiV1AsyncMock m_IdentityApiV1AsyncMock = new();

    [SetUp]
    public void SetUp()
    {
        m_ValidatorObject = new();
        m_AuthenticationServiceObject = new();
        m_ExpectedEnvironments = new()
        {
            new(),
            new(name: k_TestEnvironmentName),
        };
        m_IdentityApiV1AsyncMock.Response.Results = m_ExpectedEnvironments;
        m_IdentityApiV1AsyncMock.SetUpIdentityApiV1Async();
        m_EnvService = new EnvironmentService(m_IdentityApiV1AsyncMock.DefaultApiAsyncObject.Object, m_ValidatorObject.Object, m_AuthenticationServiceObject.Object);
        m_AuthenticationServiceObject.Setup(a => a.GetAccessTokenAsync(CancellationToken.None))
            .Returns(Task.FromResult(k_TestAccessToken));
    }

    [Test]
    public async Task AuthorizeEnvironmentService()
    {
        await m_EnvService!.AuthorizeEnvironmentService(CancellationToken.None);
        m_AuthenticationServiceObject.Verify(a => a.GetAccessTokenAsync(CancellationToken.None));
        Assert.AreEqual(k_TestAccessToken.ToHeaderValue(),
            m_IdentityApiV1AsyncMock.DefaultApiAsyncObject.Object.Configuration.DefaultHeaders[AccessTokenHelper.HeaderKey]);
    }

    [Test]
    public async Task ListAsync_ValidProjectIdGetExpectedEnvironmentList()
    {
        string mockErrorMsg;
        m_ValidatorObject.Setup(v => v.IsConfigValid(It.IsAny<string>(), It.IsAny<string>(), out mockErrorMsg))
            .Returns(true);

        var actualEnvironments = await m_EnvService!.ListAsync(k_TestProjectId, CancellationToken.None);

        CollectionAssert.AreEqual(m_ExpectedEnvironments, actualEnvironments);
        Assert.IsTrue(m_IdentityApiV1AsyncMock.IsUnityGetEnvironmentsAsyncCalled);
    }

    [Test]
    public void ListAsync_InvalidProjectIdThrowsConfigValidationException()
    {
        string invalidProjectId = "foo";
        m_ValidatorObject.Setup(v => v.ThrowExceptionIfConfigInvalid(Keys.ConfigKeys.ProjectId, invalidProjectId))
            .Throws(new ConfigValidationException(Keys.ConfigKeys.ProjectId, invalidProjectId, It.IsAny<string>()));
        Assert.ThrowsAsync<ConfigValidationException>(() => m_EnvService!.ListAsync(invalidProjectId, CancellationToken.None));
        Assert.IsFalse(m_IdentityApiV1AsyncMock.IsUnityGetEnvironmentsAsyncCalled);
    }

    [TestCase(Keys.ConfigKeys.ProjectId, k_TestProjectId)]
    [TestCase(Keys.ConfigKeys.EnvironmentId, k_TestEnvironmentId)]
    public void DeleteAsync_ValidationFailThrowException(string key, string value)
    {
        m_ValidatorObject.Setup(v => v.ThrowExceptionIfConfigInvalid(key, value))
            .Throws(new ConfigValidationException(key, value, It.IsAny<string>()));

        Assert.ThrowsAsync<ConfigValidationException>(async () => await m_EnvService!.DeleteAsync(k_TestProjectId, k_TestEnvironmentId, CancellationToken.None));
        Assert.IsFalse(m_IdentityApiV1AsyncMock.IsUnityDeleteEnvironmentAsyncCalled);
    }

    [Test]
    public void DeleteAsync_ValidationPassNoException()
    {
        string mockErrorMsg;
        m_ValidatorObject.Setup(v => v.IsConfigValid(Keys.ConfigKeys.ProjectId, It.IsAny<string>(), out mockErrorMsg))
            .Returns(true);
        m_ValidatorObject.Setup(v => v.IsConfigValid(Keys.ConfigKeys.EnvironmentId, It.IsAny<string>(), out mockErrorMsg))
            .Returns(true);

        Assert.DoesNotThrowAsync(async () => await m_EnvService!.DeleteAsync(k_TestProjectId, k_TestEnvironmentId, CancellationToken.None));
        Assert.IsTrue(m_IdentityApiV1AsyncMock.IsUnityDeleteEnvironmentAsyncCalled);
    }

    [TestCase(Keys.ConfigKeys.ProjectId, k_TestProjectId)]
    [TestCase(Keys.ConfigKeys.EnvironmentName, k_TestEnvironmentName)]
    public void AddAsync_ProjectIdEnvironmentValidationFail_ThrowException(string key, string value)
    {
        m_ValidatorObject.Setup(v => v.ThrowExceptionIfConfigInvalid(key, value))
            .Throws(new ConfigValidationException(key, value, It.IsAny<string>()));
        Assert.ThrowsAsync<ConfigValidationException>(async () => await m_EnvService!.AddAsync(k_TestEnvironmentName, k_TestProjectId, CancellationToken.None));
        Assert.IsFalse(m_IdentityApiV1AsyncMock.IsUnityAddEnvironmentAsyncCalled);
    }

    [Test]
    public void AddAsync_ProjectIdEnvironmentValidationPass()
    {
        var mockErrorMsg = "";
        m_ValidatorObject.Setup(v => v.IsConfigValid(Keys.ConfigKeys.ProjectId, It.IsAny<string>(), out mockErrorMsg))
            .Returns(true);
        m_ValidatorObject.Setup(v => v.IsConfigValid(Keys.ConfigKeys.EnvironmentId, It.IsAny<string>(), out mockErrorMsg))
            .Returns(true);
        Assert.DoesNotThrowAsync(async () => await m_EnvService!.AddAsync(k_TestProjectId, k_TestEnvironmentName, CancellationToken.None));
        Assert.IsTrue(m_IdentityApiV1AsyncMock.IsUnityAddEnvironmentAsyncCalled);
    }
}
