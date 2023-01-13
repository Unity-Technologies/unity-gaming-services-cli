using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using Unity.Services.Cli.CloudCode.Parameters;
using Unity.Services.Cli.CloudCode.Service;
using Unity.Services.Cli.CloudCode.UnitTest.Mock;
using Unity.Services.Cli.CloudCode.UnitTest.Utils;
using Unity.Services.Cli.Common.Exceptions;
using Unity.Services.Cli.Common.Models;
using Unity.Services.Cli.Common.Validator;
using Unity.Services.Cli.ServiceAccountAuthentication;
using Unity.Services.Cli.ServiceAccountAuthentication.Token;
using Unity.Services.Gateway.CloudCodeApiV1.Generated.Model;

namespace Unity.Services.Cli.CloudCode.UnitTest.Service;

[TestFixture]
class CloudCodeServiceTests
{
    const string k_TestScriptName = "test-script";
    const string k_TestAccessToken = "test-token";
    const string k_InvalidProjectId = "invalidProject";
    const string k_InvalidEnvironmentId = "foo";
    const string k_NonEmptyCode = "non-empty-code";
    const string k_NonEmptyParam = "param";

    readonly Mock<IConfigurationValidator> m_ValidatorObject = new();
    readonly Mock<IServiceAccountAuthenticationService> m_AuthenticationServiceObject = new();
    readonly Mock<ICloudCodeScriptParser> m_CloudCodeScriptParser = new();
    readonly Mock<ICloudScriptParametersParser> m_CloudScriptParametersParser = new();
    readonly CloudCodeApiV1AsyncMock m_CloudCodeApiV1AsyncMock = new();

    CloudCodeService? m_CloudCodeService;
    List<ListScriptsResponseResultsInner>? m_ExpectedScripts;
    GetScriptResponse? m_ExpectedGetScript;

    [SetUp]
    public void SetUp()
    {
        m_ValidatorObject.Reset();
        m_AuthenticationServiceObject.Reset();
        m_CloudCodeScriptParser.Reset();
        m_CloudScriptParametersParser.Reset();
        m_AuthenticationServiceObject.Setup(a => a.GetAccessTokenAsync(CancellationToken.None))
            .Returns(Task.FromResult(k_TestAccessToken));

        m_ExpectedScripts = new List<ListScriptsResponseResultsInner>
        {
            new(
                k_TestScriptName,
                ScriptType.API,
                Language.JS,
                lastPublishedDate: DateTime.Now,
                published: false,
                lastPublishedVersion: 0)
        };
        m_CloudCodeApiV1AsyncMock.ListResponse.Results = m_ExpectedScripts;

        m_ExpectedGetScript = new GetScriptResponse(
            "foo",
            ScriptType.API,
            Language.JS,
            new GetScriptResponseActiveScript("bar", 1, DateTime.Now, new List<ScriptParameter>()),
            _params: new List<ScriptParameter>(),
            versions: new List<GetScriptResponseVersionsInner>
            {
                new("bar", 1)
            });
        m_CloudCodeApiV1AsyncMock.GetResponse = m_ExpectedGetScript!;
        m_CloudCodeApiV1AsyncMock.SetUp();

        m_CloudCodeService = new CloudCodeService(
            m_CloudCodeApiV1AsyncMock.DefaultApiAsyncObject.Object,
            m_ValidatorObject.Object,
            m_AuthenticationServiceObject.Object,
            m_CloudScriptParametersParser.Object,
            m_CloudCodeScriptParser.Object);
    }

    [Test]
    public async Task AuthorizeCloudCodeService()
    {
        await m_CloudCodeService!.AuthorizeService(CancellationToken.None);
        m_AuthenticationServiceObject.Verify(a => a.GetAccessTokenAsync(CancellationToken.None));
        Assert.AreEqual(
            k_TestAccessToken.ToHeaderValue(),
            m_CloudCodeApiV1AsyncMock.DefaultApiAsyncObject.Object.Configuration.DefaultHeaders[
                AccessTokenHelper.HeaderKey]);
    }

    [Test]
    public async Task ListAsync_EmptyListSuccess()
    {
        string mockErrorMsg;
        m_ValidatorObject.Setup(v => v.IsConfigValid(It.IsAny<string>(), It.IsAny<string>(), out mockErrorMsg))
            .Returns(true);
        m_ExpectedScripts!.Clear();

        var actualScripts = await m_CloudCodeService!.ListAsync(
            TestValues.ValidProjectId, TestValues.ValidEnvironmentId, CancellationToken.None);

        Assert.AreEqual(0, actualScripts.Count());
    }

    [Test]
    public async Task ListAsync_ValidParamsGetExpectedScriptList()
    {
        string mockErrorMsg;
        m_ValidatorObject.Setup(v => v.IsConfigValid(It.IsAny<string>(), It.IsAny<string>(), out mockErrorMsg))
            .Returns(true);

        var actualScripts = await m_CloudCodeService!.ListAsync(
            TestValues.ValidProjectId, TestValues.ValidEnvironmentId, CancellationToken.None);

        CollectionAssert.AreEqual(m_ExpectedScripts, actualScripts);
        m_CloudCodeApiV1AsyncMock.DefaultApiAsyncObject.Verify(
            a => a.ListScriptsAsync(
                TestValues.ValidProjectId,
                TestValues.ValidEnvironmentId,
                CloudCodeService.k_ListLimit,
                It.IsAny<string?>(),
                0,
                CancellationToken.None),
            Times.Once);
    }

    [Test]
    public async Task ListAsync_GetMoreThanLimitScriptListSucceed()
    {
        var withinLimitScriptPattern = new ListScriptsResponseResultsInner(
            "a",
            ScriptType.API,
            Language.JS,
            lastPublishedDate: DateTime.Now,
            published: false,
            lastPublishedVersion: 0);

        const string limitName = "b";
        var limitScriptPattern = new ListScriptsResponseResultsInner(
            limitName,
            ScriptType.API,
            Language.JS,
            lastPublishedDate: DateTime.Now,
            published: false,
            lastPublishedVersion: 0);

        var withinLimitScripts = new List<ListScriptsResponseResultsInner>();
        withinLimitScripts.AddRange(Enumerable.Repeat(withinLimitScriptPattern, CloudCodeService.k_ListLimit - 1));
        withinLimitScripts.Add(limitScriptPattern);
        var withinLimitResponse = new ListScriptsResponse(withinLimitScripts, new ListScriptsResponseLinks(""));
        m_CloudCodeApiV1AsyncMock.DefaultApiAsyncObject.Setup(
                a => a.ListScriptsAsync(
                    TestValues.ValidProjectId,
                    TestValues.ValidEnvironmentId,
                    CloudCodeService.k_ListLimit,
                    null,
                    0,
                    CancellationToken.None))
            .ReturnsAsync(withinLimitResponse);

        var exceedLimitScriptPattern = new ListScriptsResponseResultsInner(
            "c",
            ScriptType.API,
            Language.JS,
            lastPublishedDate: DateTime.Now,
            published: false,
            lastPublishedVersion: 0);
        var exceedLimitScripts = new List<ListScriptsResponseResultsInner>();
        exceedLimitScripts.AddRange(
            new[]
            {
                limitScriptPattern,
                exceedLimitScriptPattern
            });
        var exceedLimitResponse = new ListScriptsResponse(
            exceedLimitScripts.ToList(), new ListScriptsResponseLinks(""));
        m_CloudCodeApiV1AsyncMock.DefaultApiAsyncObject.Setup(
                a => a.ListScriptsAsync(
                    TestValues.ValidProjectId,
                    TestValues.ValidEnvironmentId,
                    CloudCodeService.k_ListLimit,
                    limitName,
                    0,
                    CancellationToken.None))
            .ReturnsAsync(exceedLimitResponse);
        var expectedScripts = withinLimitScripts.SkipLast(1).Concat(exceedLimitScripts).ToList();

        string mockErrorMsg;
        m_ValidatorObject.Setup(v => v.IsConfigValid(It.IsAny<string>(), It.IsAny<string>(), out mockErrorMsg))
            .Returns(true);
        var actualScripts = await m_CloudCodeService!.ListAsync(
            TestValues.ValidProjectId, TestValues.ValidEnvironmentId, CancellationToken.None);

        CollectionAssert.AreEqual(expectedScripts, actualScripts);
        m_CloudCodeApiV1AsyncMock.DefaultApiAsyncObject.Verify(
            a =>
                a.ListScriptsAsync(
                    TestValues.ValidProjectId,
                    TestValues.ValidEnvironmentId,
                    CloudCodeService.k_ListLimit,
                    It.IsAny<string?>(),
                    0,
                    CancellationToken.None),
            Times.AtLeastOnce);
    }

    [Test]
    public void ListAsync_InvalidProjectIDThrowConfigValidationException()
    {
        m_ValidatorObject.Setup(v => v.ThrowExceptionIfConfigInvalid(Keys.ConfigKeys.ProjectId, k_InvalidProjectId))
            .Throws(new ConfigValidationException(Keys.ConfigKeys.ProjectId, k_InvalidProjectId, It.IsAny<string>()));
        Assert.ThrowsAsync<ConfigValidationException>(
            () => m_CloudCodeService!.ListAsync(
                k_InvalidProjectId, TestValues.ValidEnvironmentId, CancellationToken.None));
        m_CloudCodeApiV1AsyncMock.DefaultApiAsyncObject.Verify(
            a => a.ListScriptsAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<int?>(),
                It.IsAny<string?>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public void ListAsync_InvalidEnvironmentThrowConfigValidationException()
    {
        m_ValidatorObject.Setup(
                v => v.ThrowExceptionIfConfigInvalid(Keys.ConfigKeys.EnvironmentId, k_InvalidEnvironmentId))
            .Throws(new ConfigValidationException(Keys.ConfigKeys.EnvironmentId, k_InvalidEnvironmentId, It.IsAny<string>()));

        Assert.ThrowsAsync<ConfigValidationException>(
            () => m_CloudCodeService!.ListAsync(
                TestValues.ValidProjectId, k_InvalidEnvironmentId, CancellationToken.None));
        m_CloudCodeApiV1AsyncMock.DefaultApiAsyncObject.Verify(
            a => a.ListScriptsAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<int?>(),
                It.IsAny<string?>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public async Task PublishAsync_CalledWithValidParams()
    {
        string mockErrorMsg;
        m_ValidatorObject.Setup(v => v.IsConfigValid(It.IsAny<string>(), It.IsAny<string>(), out mockErrorMsg))
            .Returns(true);

        await m_CloudCodeService!.PublishAsync(
            TestValues.ValidProjectId,
            TestValues.ValidEnvironmentId,
            CloudCodeApiV1AsyncMock.PublishScriptAsyncScriptName,
            m_CloudCodeApiV1AsyncMock.PublishScriptAsyncRequestPayload._Version,
            CancellationToken.None);

        var payload = new PublishScriptRequest
        {
            _Version = m_CloudCodeApiV1AsyncMock.PublishScriptAsyncRequestPayload._Version
        };
        m_CloudCodeApiV1AsyncMock.DefaultApiAsyncObject.Verify(
            a => a.PublishScriptAsync(
                TestValues.ValidProjectId,
                TestValues.ValidEnvironmentId,
                CloudCodeApiV1AsyncMock.PublishScriptAsyncScriptName,
                payload,
                0,
                default),
            Times.Once);
    }

    [Test]
    public void PublishAsync_InvalidProjectIDThrowConfigValidationException()
    {
        m_ValidatorObject.Setup(v => v.ThrowExceptionIfConfigInvalid(Keys.ConfigKeys.ProjectId, k_InvalidProjectId))
            .Throws(new ConfigValidationException(Keys.ConfigKeys.ProjectId, k_InvalidProjectId, It.IsAny<string>()));

        Assert.ThrowsAsync<ConfigValidationException>(
            () => m_CloudCodeService!.PublishAsync(
                k_InvalidProjectId,
                TestValues.ValidEnvironmentId,
                CloudCodeApiV1AsyncMock.PublishScriptAsyncScriptName,
                m_CloudCodeApiV1AsyncMock.PublishScriptAsyncRequestPayload._Version,
                CancellationToken.None));

        m_CloudCodeApiV1AsyncMock.DefaultApiAsyncObject.Verify(
            a => a.PublishScriptAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<PublishScriptRequest>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public void PublishAsync_InvalidEnvironmentThrowConfigValidationException()
    {
        m_ValidatorObject.Setup(
                v => v.ThrowExceptionIfConfigInvalid(Keys.ConfigKeys.EnvironmentId, k_InvalidEnvironmentId))
            .Throws(
                new ConfigValidationException(
                    Keys.ConfigKeys.EnvironmentId, k_InvalidEnvironmentId, It.IsAny<string>()));

        Assert.ThrowsAsync<ConfigValidationException>(
            () => m_CloudCodeService!.PublishAsync(
                TestValues.ValidProjectId,
                k_InvalidEnvironmentId,
                CloudCodeApiV1AsyncMock.PublishScriptAsyncScriptName,
                m_CloudCodeApiV1AsyncMock.PublishScriptAsyncRequestPayload._Version,
                CancellationToken.None));

        m_CloudCodeApiV1AsyncMock.DefaultApiAsyncObject.Verify(
            a => a.PublishScriptAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<PublishScriptRequest>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("!%")]
    public void PublishAsync_InvalidScriptNameThrowCliException(string invalidScriptName)
    {
        Assert.ThrowsAsync<CliException>(
            () => m_CloudCodeService!.PublishAsync(
                TestValues.ValidProjectId,
                k_InvalidEnvironmentId,
                invalidScriptName,
                m_CloudCodeApiV1AsyncMock.PublishScriptAsyncRequestPayload._Version,
                CancellationToken.None));
        m_ValidatorObject.Verify(
            v => v.ThrowExceptionIfConfigInvalid(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Test]
    public void DeleteAsync_InvalidProjectIDThrowConfigValidationException()
    {
        m_ValidatorObject.Setup(
                ex => ex.ThrowExceptionIfConfigInvalid(Keys.ConfigKeys.ProjectId, k_InvalidProjectId))
            .Throws(new ConfigValidationException(Keys.ConfigKeys.ProjectId, k_InvalidProjectId, It.IsAny<string>()));
        Assert.ThrowsAsync<ConfigValidationException>(
            () => m_CloudCodeService!.ListAsync(
                k_InvalidProjectId, TestValues.ValidEnvironmentId, CancellationToken.None));
    }

    [Test]
    public void DeleteAsync_InvalidEnvironmentOrProjectIdThrowsConfigValidationException()
    {
        m_ValidatorObject.Setup(ex => ex.ThrowExceptionIfConfigInvalid(It.IsAny<string>(), It.IsAny<string>()))
            .Throws(new ConfigValidationException(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()));

        Assert.ThrowsAsync<ConfigValidationException>(
            () => m_CloudCodeService!.DeleteAsync(It.IsAny<string>(), It.IsAny<string>(), k_TestScriptName));

        m_CloudCodeApiV1AsyncMock.DefaultApiAsyncObject.Verify(
            ex => ex.DeleteScriptWithHttpInfoAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [TestCase("")]
    [TestCase("test.java")]
    [TestCase("test/java")]
    public void DeleteAsync_InvalidScriptNameThrowsCliException(string scriptName)
    {
        m_ValidatorObject.Setup(ex => ex.ThrowExceptionIfConfigInvalid(It.IsAny<string>(), It.IsAny<string>()));

        Assert.ThrowsAsync<CliException>(
            () => m_CloudCodeService!.DeleteAsync(It.IsAny<string>(), It.IsAny<string>(), scriptName));

        m_CloudCodeApiV1AsyncMock.DefaultApiAsyncObject.Verify(
            ex => ex.DeleteScriptWithHttpInfoAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public void DeleteAsync_ValidInputCallsDeleteScriptWithHttpInfoAsync()
    {
        m_ValidatorObject.Setup(ex => ex.ThrowExceptionIfConfigInvalid(It.IsAny<string>(), It.IsAny<string>()));

        Assert.DoesNotThrowAsync(
            () => m_CloudCodeService!.DeleteAsync(It.IsAny<string>(), It.IsAny<string>(), k_TestScriptName));

        m_CloudCodeApiV1AsyncMock.DefaultApiAsyncObject.Verify(
            ex => ex.DeleteScriptWithHttpInfoAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                k_TestScriptName,
                0,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task GetAsync_ValidParamsGetExpectedScriptList()
    {
        string mockErrorMsg;
        m_ValidatorObject.Setup(v => v.IsConfigValid(It.IsAny<string>(), It.IsAny<string>(), out mockErrorMsg))
            .Returns(true);

        var actualScript = await m_CloudCodeService!.GetAsync(
            TestValues.ValidProjectId, TestValues.ValidEnvironmentId, k_TestScriptName, CancellationToken.None);

        Assert.AreEqual(m_ExpectedGetScript, actualScript);

        m_CloudCodeApiV1AsyncMock.DefaultApiAsyncObject.Verify(
            a => a.GetScriptAsync(
                TestValues.ValidProjectId,
                TestValues.ValidEnvironmentId,
                k_TestScriptName,
                0,
                CancellationToken.None),
            Times.Once);
    }

    [Test]
    public void GetAsync_InvalidProjectIDThrowConfigValidationException()
    {
        m_ValidatorObject.Setup(v => v.ThrowExceptionIfConfigInvalid(Keys.ConfigKeys.ProjectId, k_InvalidProjectId))
            .Throws(new ConfigValidationException(Keys.ConfigKeys.ProjectId, k_InvalidProjectId, It.IsAny<string>()));

        Assert.ThrowsAsync<ConfigValidationException>(
            () => m_CloudCodeService!.GetAsync(
                k_InvalidProjectId, TestValues.ValidEnvironmentId, k_TestScriptName, CancellationToken.None));

        m_CloudCodeApiV1AsyncMock.DefaultApiAsyncObject.Verify(
            a => a.GetScriptAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public void GetAsync_InvalidEnvironmentThrowConfigValidationException()
    {
        m_ValidatorObject.Setup(v => v.ThrowExceptionIfConfigInvalid(Keys.ConfigKeys.EnvironmentId, k_InvalidEnvironmentId))
            .Throws(new ConfigValidationException(Keys.ConfigKeys.EnvironmentId, k_InvalidEnvironmentId, It.IsAny<string>()));

        Assert.ThrowsAsync<ConfigValidationException>(
            () => m_CloudCodeService!.GetAsync(
                TestValues.ValidProjectId, k_InvalidEnvironmentId, k_TestScriptName, CancellationToken.None));

        m_CloudCodeApiV1AsyncMock.DefaultApiAsyncObject.Verify(
            a => a.GetScriptAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("!%")]
    public void GetAsync_InvalidScriptNameThrowCliException(string invalidScriptName)
    {
        Assert.ThrowsAsync<CliException>(
            () => m_CloudCodeService!.GetAsync(
                TestValues.ValidProjectId, k_InvalidEnvironmentId, invalidScriptName, CancellationToken.None));
        m_ValidatorObject.Verify(
            v => v.ThrowExceptionIfConfigInvalid(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Test]
    public void CreateAsync_InvalidEnvironmentOrProjectIdThrowsConfigValidationException()
    {
        m_ValidatorObject.Setup(
                ex => ex.ThrowExceptionIfConfigInvalid(It.IsAny<string>(), It.IsAny<string>()))
            .Throws(new ConfigValidationException(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()));

        Assert.ThrowsAsync<ConfigValidationException>(
            () => m_CloudCodeService!.CreateAsync(
                k_InvalidProjectId,
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<ScriptType>(),
                It.IsAny<Language>(),
                It.IsAny<string>(),
                CancellationToken.None));
    }

    [TestCase("test.java", "code")]
    [TestCase("test", null)]
    public void CreateAsync_InvalidScriptNameOrCodeThrowsConfigValidationException(string? scriptName, string? code)
    {
        m_ValidatorObject.Setup(
            ex => ex.ThrowExceptionIfConfigInvalid(It.IsAny<string>(), It.IsAny<string>()));

        Assert.ThrowsAsync<CliException>(
            () => m_CloudCodeService!.CreateAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                scriptName,
                It.IsAny<ScriptType>(),
                It.IsAny<Language>(),
                code,
                CancellationToken.None));
    }

    [Test]
    public void CreateAsync_ValidInputCallsCreateScript()
    {
        m_ValidatorObject.Setup(
            ex => ex.ThrowExceptionIfConfigInvalid(It.IsAny<string>(), It.IsAny<string>()));

        var scriptParamList = new List<ScriptParameter>
        {
            new("foo")
        };

        var createRequest = new CreateScriptRequest(
            k_TestScriptName, ScriptType.API, scriptParamList, k_NonEmptyCode, Language.JS);

        m_CloudCodeScriptParser.Setup(
                parser => parser.ParseToScriptParamsJsonAsync(k_NonEmptyCode, CancellationToken.None))
            .ReturnsAsync(k_NonEmptyParam);
        m_CloudScriptParametersParser.Setup(parser => parser.ParseToScriptParameters(k_NonEmptyParam))
            .Returns(scriptParamList);
        Assert.DoesNotThrowAsync(
            () => m_CloudCodeService!.CreateAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                k_TestScriptName,
                ScriptType.API,
                Language.JS,
                k_NonEmptyCode,
                CancellationToken.None));

        m_CloudCodeApiV1AsyncMock.DefaultApiAsyncObject.Verify(
            ex => ex.CreateScriptAsync(
                It.IsAny<string>(), It.IsAny<string>(), createRequest, 0, CancellationToken.None),
            Times.Once);
    }

    [Test]
    public void UpdateAsync_InvalidEnvironmentOrProjectIdThrowsConfigValidationException()
    {
        m_ValidatorObject.Setup(ex => ex.ThrowExceptionIfConfigInvalid(It.IsAny<string>(), It.IsAny<string>()))
            .Throws(new ConfigValidationException(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()));

        Assert.ThrowsAsync<ConfigValidationException>(
            () => m_CloudCodeService!.UpdateAsync(
                k_InvalidProjectId,
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                CancellationToken.None));
    }

    [TestCase("test.java", "code")]
    [TestCase("test", null)]
    public void UpdateAsync_InvalidScriptNameOrCodeThrowsConfigValidationException(string? scriptName, string? code)
    {
        m_ValidatorObject.Setup(ex => ex.ThrowExceptionIfConfigInvalid(It.IsAny<string>(), It.IsAny<string>()));

        Assert.ThrowsAsync<CliException>(
            () => m_CloudCodeService!.UpdateAsync(
                It.IsAny<string>(), It.IsAny<string>(), scriptName, code, CancellationToken.None));
        m_CloudCodeApiV1AsyncMock.DefaultApiAsyncObject.Verify(
            ex => ex.UpdateScriptAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<UpdateScriptRequest>(),
                It.IsAny<int>(),
                CancellationToken.None),
            Times.Never);
    }

    [Test]
    public void UpdateAsync_ValidInputCallsUpdateScript()
    {
        m_ValidatorObject.Setup(ex => ex.ThrowExceptionIfConfigInvalid(It.IsAny<string>(), It.IsAny<string>()));

        var scriptParamList = new List<ScriptParameter>
        {
            new("foo")
        };

        m_CloudCodeScriptParser.Setup(
                parser => parser.ParseToScriptParamsJsonAsync(k_NonEmptyCode, CancellationToken.None))
            .ReturnsAsync(k_NonEmptyParam);
        m_CloudScriptParametersParser.Setup(parser => parser.ParseToScriptParameters(k_NonEmptyParam))
            .Returns(scriptParamList);
        Assert.DoesNotThrowAsync(
            () => m_CloudCodeService!.UpdateAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                k_TestScriptName,
                k_NonEmptyCode,
                CancellationToken.None));

        var scriptRequest = new UpdateScriptRequest(scriptParamList, k_NonEmptyCode);
        m_CloudCodeApiV1AsyncMock.DefaultApiAsyncObject.Verify(
            ex => ex.UpdateScriptAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                scriptRequest,
                0,
                CancellationToken.None),
            Times.Once);
    }
}
