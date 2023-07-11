using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using Unity.Services.Cli.Common.Exceptions;
using Unity.Services.Cli.Common.Models;
using Unity.Services.Cli.Common.Validator;
using Unity.Services.Gateway.IdentityApiV1.Generated.Model;

namespace Unity.Services.Cli.Environment.UnitTest.Mock;

[TestFixture]
public class UnityEnvironmentTests
{
    Mock<IEnvironmentService> m_MockEnvService = new();
    Mock<IConfigurationValidator> m_ConfigurationValidator = new();
    UnityEnvironment? m_UnityEnvironment;
    const string k_ValidProjectId = "00000000-0000-0000-0000-000000000000";
    const string k_ValidEnvironmentName = "staging";

    [SetUp]
    public void SetUp()
    {
        m_UnityEnvironment = new UnityEnvironment(m_MockEnvService.Object, m_ConfigurationValidator.Object);
    }

    [TearDown]
    public void TearDown()
    {
        m_MockEnvService.Reset();
    }

    [TestCase(k_ValidEnvironmentName, null)]
    [TestCase(null, k_ValidProjectId)]
    [TestCase(null, null)]
    public void FetchIdentifierAsyncThrowsExceptionWhenMissingConfig(string? name, string? projectId)
    {
        m_ConfigurationValidator.Setup(c => c.ThrowExceptionIfConfigInvalid(Keys.ConfigKeys.EnvironmentName, null!))
            .Throws(new MissingConfigurationException(
                Keys.ConfigKeys.EnvironmentName, Keys.EnvironmentKeys.EnvironmentName));

        m_UnityEnvironment!.SetName(name);
        m_UnityEnvironment!.SetProjectId(projectId);

        Assert.ThrowsAsync<MissingConfigurationException>(() => m_UnityEnvironment!.FetchIdentifierAsync(CancellationToken.None));
    }

    [Test]
    public void FetchIdentifierAsyncThrowsExceptionWhenEnvironmentNameNotFound()
    {
        IEnumerable<EnvironmentResponse> responses = new[]
        {
            new EnvironmentResponse()
        };

        m_UnityEnvironment!.SetName(k_ValidEnvironmentName);
        m_UnityEnvironment!.SetProjectId(k_ValidProjectId);
        m_MockEnvService.Setup(a => a.ListAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(responses));

        Assert.ThrowsAsync<EnvironmentNotFoundException>(() => m_UnityEnvironment!.FetchIdentifierAsync(CancellationToken.None));
    }

    [Test]
    public async Task FetchIdentifierAsyncReturnsIdWhenMatchesEnvironmentName()
    {
        string mockEnvironmentId = "00000000-0000-0000-0000-000000000001";
        IEnumerable<EnvironmentResponse> responses = new[]
        {
            new EnvironmentResponse()
            {
                Name = k_ValidEnvironmentName,
                Id = new Guid(mockEnvironmentId)
            }
        };

        m_UnityEnvironment!.SetName(k_ValidEnvironmentName);
        m_UnityEnvironment!.SetProjectId(k_ValidProjectId);
        m_MockEnvService.Setup(a =>
                a.ListAsync(k_ValidProjectId, It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(responses));

        var result = await m_UnityEnvironment!.FetchIdentifierAsync(CancellationToken.None);
        Assert.AreEqual(mockEnvironmentId, result!);
    }
}
