using System;
using System.Collections.Generic;
using System.Net;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Newtonsoft.Json;
using NUnit.Framework;
using Unity.Services.Cli.CloudCode.Deploy;
using Unity.Services.Cli.CloudCode.Model;
using Unity.Services.Cli.CloudCode.Service;
using Unity.Services.Cli.CloudCode.UnitTest.Utils;
using Unity.Services.CloudCode.Authoring.Editor.Core.Model;
using Unity.Services.Gateway.CloudCodeApiV1.Generated.Client;
using Unity.Services.Gateway.CloudCodeApiV1.Generated.Model;
using Language = Unity.Services.Gateway.CloudCodeApiV1.Generated.Model.Language;

namespace Unity.Services.Cli.CloudCode.UnitTest.Deploy;

[TestFixture]
class CloudCodeScriptClientTests
{
    const string k_File = "script.js";
    const string k_ScriptName = "script";
    const string k_Script = "module.exports.params = { sides: \"NUMERIC\"};";

    readonly Mock<ICloudCodeService> m_MockCcService = new();
    readonly Mock<ICloudCodeInputParser> m_MockCcInputParser = new();
    readonly CloudCodeScriptClient m_CloudCodeScriptClient;
    readonly IScript m_Script = new CloudCodeScript(
        ScriptName.FromPath(k_File),
        Services.CloudCode.Authoring.Editor.Core.Model.Language.JS,
        k_File,
        k_Script,
        new List<CloudCodeParameter>(),
        "");

    public CloudCodeScriptClientTests()
    {
        m_CloudCodeScriptClient = new(
            m_MockCcService.Object,
            m_MockCcInputParser.Object,
            TestValues.ValidProjectId,
            TestValues.ValidEnvironmentId,
            CancellationToken.None);
    }

    [SetUp]
    public void SetUp()
    {
        m_MockCcService.Reset();
        m_MockCcInputParser.Reset();
        m_MockCcInputParser.Setup(c => c.LoadScriptCodeAsync(k_File, CancellationToken.None))
            .ReturnsAsync(k_Script);

        m_CloudCodeScriptClient.EnvironmentId = TestValues.ValidEnvironmentId;
        m_CloudCodeScriptClient.ProjectId = TestValues.ValidProjectId;
        m_CloudCodeScriptClient.CancellationToken = CancellationToken.None;
    }

    [Test]
    public async Task UploadFromFileCreateSucceed()
    {
        m_MockCcService.Setup(
                c => c.UpdateAsync(TestValues.ValidProjectId, TestValues.ValidEnvironmentId, k_ScriptName, k_Script, CancellationToken.None))
            .Throws(() => new ApiException((int)HttpStatusCode.NotFound, ""));

        var scriptName = await m_CloudCodeScriptClient.UploadFromFile(m_Script);

        m_MockCcService.Verify(
            c => c.UpdateAsync(TestValues.ValidProjectId, TestValues.ValidEnvironmentId, k_ScriptName, k_Script, CancellationToken.None),
            Times.Once);
        m_MockCcService.Verify(
            c => c.CreateAsync(
                TestValues.ValidProjectId,
                TestValues.ValidEnvironmentId,
                k_ScriptName,
                ScriptType.API,
                Language.JS,
                k_Script,
                CancellationToken.None),
            Times.Once);
        Assert.AreEqual(k_ScriptName, scriptName.GetNameWithoutExtension());
    }

    [Test]
    public async Task UploadFromFileUpdateSucceed()
    {
        var scriptName = await m_CloudCodeScriptClient.UploadFromFile(m_Script);

        m_MockCcService.Verify(
            c => c.UpdateAsync(TestValues.ValidProjectId, TestValues.ValidEnvironmentId, k_ScriptName, k_Script, CancellationToken.None),
            Times.Once);
        m_MockCcService.Verify(
            c => c.CreateAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<ScriptType>(),
                It.IsAny<Language>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
        Assert.AreEqual(k_ScriptName, scriptName.GetNameWithoutExtension());
    }

    [Test]
    public void UploadFromFileUpdateThrowException()
    {
        m_MockCcService.Setup(
                c => c.UpdateAsync(TestValues.ValidProjectId, TestValues.ValidEnvironmentId, k_ScriptName, k_Script, CancellationToken.None))
            .Throws(() => new ApiException((int)HttpStatusCode.Unauthorized, ""));

        Assert.ThrowsAsync<ApiException>(() => m_CloudCodeScriptClient.UploadFromFile(m_Script));

        m_MockCcService.Verify(
            c => c.UpdateAsync(TestValues.ValidProjectId, TestValues.ValidEnvironmentId, k_ScriptName, k_Script, CancellationToken.None),
            Times.Once);
        m_MockCcService.Verify(
            c => c.CreateAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<ScriptType>(),
                It.IsAny<Language>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public async Task ListScriptsSucceed()
    {
        var expectedScripts = new List<ListScriptsResponseResultsInner>
        {
            new(
                k_ScriptName,
                ScriptType.API,
                Language.JS,
                lastPublishedDate: DateTime.Now,
                published: false,
                lastPublishedVersion: 0)
        };
        m_MockCcService.Setup(c => c.ListAsync(TestValues.ValidProjectId, TestValues.ValidEnvironmentId, CancellationToken.None))
            .ReturnsAsync(expectedScripts);

        var scripts = await m_CloudCodeScriptClient.ListScripts();

        CollectionAssert.AreEqual(
            scripts.Select(script => script.Name.GetNameWithoutExtension()),
            expectedScripts.Select(script => script.Name));
        CollectionAssert.AreEqual(
            scripts.Select(script => script.LastPublishedDate),
            expectedScripts.Select(script => script.LastPublishedDate.ToString()));
    }

    [Test]
    public async Task PublishCallsPublishHandler()
    {
        var scriptName = new ScriptName(k_ScriptName);
        var expectedResult = new PublishScriptResponse();
        m_MockCcService.Setup(
                x => x.PublishAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<int>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        await m_CloudCodeScriptClient.Publish(scriptName);

        m_MockCcService.Verify(
            x => x.PublishAsync(TestValues.ValidProjectId, TestValues.ValidEnvironmentId, k_ScriptName, 0, CancellationToken.None),
            Times.Once);
    }

    [Test]
    public void PublishDoesNotFailOnDuplicateError()
    {
        var scriptName = new ScriptName(k_ScriptName);
        var duplicateError = CreateDuplicatePublishError();
        m_MockCcService.Setup(
                x => x.PublishAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<int>(),
                    It.IsAny<CancellationToken>()))
            .Throws(() => duplicateError);

        Assert.DoesNotThrowAsync(() => m_CloudCodeScriptClient.Publish(scriptName));

        m_MockCcService.Verify(
            x => x.PublishAsync(TestValues.ValidProjectId, TestValues.ValidEnvironmentId, k_ScriptName, 0, CancellationToken.None),
            Times.Once);
    }

    static ApiException CreateDuplicatePublishError()
    {
        var headers = CreateJsonProblemHeaders();
        var errorContent = new ApiJsonProblem
        {
            Code = CloudCodeScriptClient.DuplicatePublishErrorCode,
            Detail = "script is already active",
            Status = 400,
            Title = "Error",
            Type = "problems/basic",
        };
        var error = new ApiException(0, "This is a test error.", JsonConvert.SerializeObject(errorContent), headers);
        return error;
    }

    static Multimap<string, string> CreateJsonProblemHeaders()
    {
        var headers = new Multimap<string, string>
        {
            [CloudCodeScriptClient.ContentTypeHeaderKey] = new List<string>
            {
                CloudCodeScriptClient.ProblemJsonHeader,
            },
        };
        return headers;
    }

    [Test]
    public void PublishThrowsNonDuplicateJsonProblemError()
    {
        var scriptName = new ScriptName(k_ScriptName);
        var jsonProblemHeaders = CreateJsonProblemHeaders();
        var jsonProblem = new ApiException(0, "", null, jsonProblemHeaders);
        m_MockCcService.Setup(
                x => x.PublishAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<int>(),
                    It.IsAny<CancellationToken>()))
            .Throws(() => jsonProblem);

        Assert.ThrowsAsync<ApiException>(() => m_CloudCodeScriptClient.Publish(scriptName));

        m_MockCcService.Verify(
            x => x.PublishAsync(TestValues.ValidProjectId, TestValues.ValidEnvironmentId, k_ScriptName, 0, CancellationToken.None),
            Times.Once);
    }

    [Test]
    public void PublishThrowsOnOtherError()
    {
        var scriptName = new ScriptName(k_ScriptName);

        AssertServiceExceptionIsRethrown<ApiException>();
        AssertServiceExceptionIsRethrown<InvalidOperationException>();

        void AssertServiceExceptionIsRethrown<T>()
            where T : Exception, new()
        {
            m_MockCcService.Setup(
                    x => x.PublishAsync(
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.IsAny<int>(),
                        It.IsAny<CancellationToken>()))
                .Throws<T>();

            Assert.ThrowsAsync<T>(() => m_CloudCodeScriptClient.Publish(scriptName));
        }
    }

    [Test]
    public void IsDuplicatePublishErrorReturnsTrueWithJsonHeadersAndErrorContentWithSpecificErrorCode()
    {
        var duplicateError = CreateDuplicatePublishError();

        Assert.IsTrue(CloudCodeScriptClient.IsDuplicatePublishError(duplicateError));
    }

    [Test]
    public void IsDuplicatePublishErrorReturnsFalseWithoutJsonHeaders()
    {
        var error = new ApiException();

        Assert.IsFalse(CloudCodeScriptClient.IsDuplicatePublishError(error));
    }

    [Test]
    public void IsDuplicatePublishErrorReturnsFalseWithoutErrorContent()
    {
        var jsonProblemHeaders = CreateJsonProblemHeaders();
        var error = new ApiException(0, "", headers: jsonProblemHeaders);

        Assert.IsFalse(CloudCodeScriptClient.IsDuplicatePublishError(error));
    }

    [Test]
    public void IsDuplicatePublishErrorReturnsFalseWithoutErrorContentWithSpecificErrorCode()
    {
        var jsonProblemHeaders = CreateJsonProblemHeaders();
        var jsonProblem = new ApiJsonProblem
        {
            Code = 100,
        };
        var serializedJsonProblem = JsonConvert.SerializeObject(jsonProblem);
        var error = new ApiException(0, "", serializedJsonProblem, jsonProblemHeaders);

        Assert.IsFalse(CloudCodeScriptClient.IsDuplicatePublishError(error));
    }

    [Test]
    public void HasProblemJsonReturnsTrueWithSpecificHeaders()
    {
        var jsonProblemHeaders = CreateJsonProblemHeaders();
        var error = new ApiException(0, "", headers: jsonProblemHeaders);

        Assert.IsTrue(CloudCodeScriptClient.HasProblemJson(error));
    }

    [Test]
    public void HasProblemJsonReturnsFalseWithoutSpecificHeaders()
    {
        var error = new ApiException();

        Assert.IsFalse(CloudCodeScriptClient.HasProblemJson(error));
    }

    [Test]
    public async Task DeleteSucceed()
    {
        var scriptName = new ScriptName(k_ScriptName);
        await m_CloudCodeScriptClient.Delete(scriptName);
        m_MockCcService.Verify(
            c => c.DeleteAsync(TestValues.ValidProjectId, TestValues.ValidEnvironmentId, k_ScriptName, CancellationToken.None), Times.Once);
    }

    [Test]
    public async Task GetSucceed()
    {
        var expectedResponse = new GetScriptResponse(
            name: k_ScriptName,
            activeScript: new GetScriptResponseActiveScript(
                k_Script, _params: new List<ScriptParameter>
                {
                    new("sides", ScriptParameter.TypeEnum.NUMERIC)
                }),
            versions: new List<GetScriptResponseVersionsInner>());
        m_MockCcService.Setup(c => c.GetAsync(TestValues.ValidProjectId, TestValues.ValidEnvironmentId, k_ScriptName, CancellationToken.None))
            .ReturnsAsync(expectedResponse);
        var scriptName = new ScriptName(k_ScriptName);
        var script = await m_CloudCodeScriptClient.Get(scriptName);

        Assert.AreEqual(expectedResponse.ActiveScript.Code, script.Body);
        Assert.AreEqual(expectedResponse.Name, script.Name.GetNameWithoutExtension());
        CollectionAssert.AreEqual(
            expectedResponse.ActiveScript.Params.Select(p => p.Name),
            script.Parameters.Select(p => p.Name));
        CollectionAssert.AreEqual(
            expectedResponse.ActiveScript.Params.Select(p => p.Required),
            script.Parameters.Select(p => p.Required));
        CollectionAssert.AreEqual(
            expectedResponse.ActiveScript.Params.Select(p => p.ToCloudCodeParameter()),
            script.Parameters);
    }

    [Test]
    public void InitializeSetsProperties()
    {
        m_CloudCodeScriptClient.EnvironmentId = null!;
        m_CloudCodeScriptClient.ProjectId = null!;
        m_CloudCodeScriptClient.CancellationToken = CancellationToken.None;
        var expectedCancellationToken = new CancellationToken();

        m_CloudCodeScriptClient.Initialize(
            TestValues.ValidEnvironmentId, TestValues.ValidProjectId, expectedCancellationToken);

        Assert.AreEqual(TestValues.ValidEnvironmentId, m_CloudCodeScriptClient.EnvironmentId);
        Assert.AreEqual(TestValues.ValidProjectId, m_CloudCodeScriptClient.ProjectId);
        Assert.AreEqual(expectedCancellationToken, m_CloudCodeScriptClient.CancellationToken);
    }
}
