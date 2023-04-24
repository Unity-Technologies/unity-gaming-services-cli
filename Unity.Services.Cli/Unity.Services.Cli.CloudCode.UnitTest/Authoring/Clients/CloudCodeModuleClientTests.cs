using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using Unity.Services.Cli.CloudCode.Deploy;
using Unity.Services.Cli.CloudCode.Service;
using Unity.Services.Cli.CloudCode.UnitTest.Utils;
using Unity.Services.CloudCode.Authoring.Editor.Core.Model;
using Unity.Services.Gateway.CloudCodeApiV1.Generated.Client;
using Unity.Services.Gateway.CloudCodeApiV1.Generated.Model;
using APILanguage = Unity.Services.Gateway.CloudCodeApiV1.Generated.Model.Language;
using CoreLanguage = Unity.Services.CloudCode.Authoring.Editor.Core.Model.Language;

namespace Unity.Services.Cli.CloudCode.UnitTest.Authoring;

[TestFixture]
class CloudCodeModuleClientTests
{
    const string k_File = "module.zip";
    const string k_ModuleName = "module";
    readonly MemoryStream k_MemoryStream = new MemoryStream();

    readonly Mock<ICloudCodeService> m_MockService = new();
    readonly Mock<ICloudCodeInputParser> m_MockInputParser = new();
    readonly CloudCodeModuleClient m_CloudCodeModuleClient;

    readonly IScript m_Module = new CloudCode.Deploy.CloudCodeModule(
        ScriptName.FromPath(k_File),
        CoreLanguage.JS,
        k_File);

    public CloudCodeModuleClientTests()
    {
        m_CloudCodeModuleClient = new(
            m_MockService.Object,
            m_MockInputParser.Object,
            TestValues.ValidProjectId,
            TestValues.ValidEnvironmentId,
            CancellationToken.None);
    }

    [SetUp]
    public void SetUp()
    {
        m_MockService.Reset();
        m_MockInputParser.Reset();
        m_MockInputParser.Setup(c => c.LoadModuleContentsAsync(k_File))
            .ReturnsAsync(k_MemoryStream);

        m_CloudCodeModuleClient.EnvironmentId = TestValues.ValidEnvironmentId;
        m_CloudCodeModuleClient.ProjectId = TestValues.ValidProjectId;
        m_CloudCodeModuleClient.CancellationToken = CancellationToken.None;
    }

    [Test]
    public async Task UploadFromFileUpdateSucceed()
    {
        var scriptName = await m_CloudCodeModuleClient.UploadFromFile(m_Module);

        m_MockService.Verify(
            c => c.UpdateModuleAsync(TestValues.ValidProjectId, TestValues.ValidEnvironmentId, k_ModuleName, k_MemoryStream, CancellationToken.None),
            Times.Once);
        Assert.AreEqual(k_ModuleName, scriptName.GetNameWithoutExtension());
    }

    [Test]
    public void UploadFromFileUpdateThrowException()
    {
        m_MockService.Setup(
                c => c.UpdateModuleAsync(TestValues.ValidProjectId, TestValues.ValidEnvironmentId, k_ModuleName, k_MemoryStream, CancellationToken.None))
            .Throws(() => new ApiException((int)HttpStatusCode.Unauthorized, ""));

        Assert.ThrowsAsync<ApiException>(() => m_CloudCodeModuleClient.UploadFromFile(m_Module));

        m_MockService.Verify(
            c => c.UpdateModuleAsync(TestValues.ValidProjectId, TestValues.ValidEnvironmentId, k_ModuleName, k_MemoryStream, CancellationToken.None),
            Times.Once);
    }

    [Test]
    public void PublishDoesNotThrowException()
    {
        var scriptName = new ScriptName(k_ModuleName);
        Assert.DoesNotThrowAsync(() => m_CloudCodeModuleClient.Publish(scriptName));
    }

    [Test]
    public async Task DeleteModuleSucceed()
    {
        var scriptName = new ScriptName(k_ModuleName);
        await m_CloudCodeModuleClient.Delete(scriptName);
        m_MockService.Verify(
            c => c.DeleteModuleAsync(TestValues.ValidProjectId, TestValues.ValidEnvironmentId, k_ModuleName, CancellationToken.None), Times.Once);
    }

    [Test]
    public async Task GetModuleSucceed()
    {
        var apiResponse = new GetModuleResponse(
            name: k_ModuleName,
            language: APILanguage.JS);
        m_MockService.Setup(c => c.GetModuleAsync(TestValues.ValidProjectId, TestValues.ValidEnvironmentId, k_ModuleName, CancellationToken.None))
            .ReturnsAsync(apiResponse);
        var moduleName = new ScriptName(k_ModuleName);
        var actualResponse = await m_CloudCodeModuleClient.Get(moduleName);

        Assert.AreEqual(apiResponse.Name, actualResponse.Name.GetNameWithoutExtension());
        Assert.AreEqual(CoreLanguage.JS, actualResponse.Language);
    }

    [Test]
    public void InitializeSetsProperties()
    {
        m_CloudCodeModuleClient.EnvironmentId = null!;
        m_CloudCodeModuleClient.ProjectId = null!;
        m_CloudCodeModuleClient.CancellationToken = CancellationToken.None;
        var expectedCancellationToken = new CancellationToken();

        m_CloudCodeModuleClient.Initialize(
            TestValues.ValidEnvironmentId, TestValues.ValidProjectId, expectedCancellationToken);

        Assert.AreEqual(TestValues.ValidEnvironmentId, m_CloudCodeModuleClient.EnvironmentId);
        Assert.AreEqual(TestValues.ValidProjectId, m_CloudCodeModuleClient.ProjectId);
        Assert.AreEqual(expectedCancellationToken, m_CloudCodeModuleClient.CancellationToken);
    }
}
