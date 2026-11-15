using System.Net;
using Moq;
using Newtonsoft.Json;
using NUnit.Framework;
using Unity.Services.Cli.Common.Exceptions;
using Unity.Services.Cli.Common.Validator;
using Unity.Services.Cli.Player.IdProvider.Models;
using Unity.Services.Cli.Player.IdProvider.Service;
using Unity.Services.Cli.ServiceAccountAuthentication;
using Unity.Services.Gateway.PlayerIdentityApiV2.Generated.Api;
using Unity.Services.Gateway.PlayerIdentityApiV2.Generated.Client;
using Unity.Services.Gateway.PlayerIdentityApiV2.Generated.Model;

namespace Unity.Services.Cli.Player.UnitTest.IdProvider;

public class IdProviderServiceTests
{
    const string k_ProjectId = "abcd1234-ab12-cd34-ef56-abcdef123456";
    const string k_ConfigId = "8bdacc33-6eef-4577-beb0-633c86259f5b";
    const string k_Secret = "super-secret-steam-key";

    static readonly Guid k_Project = Guid.Parse(k_ProjectId);
    static readonly Guid k_Config = Guid.Parse(k_ConfigId);

    readonly Mock<IAuthenticationProvidersApiAsync> m_Api = new();
    readonly Mock<IServiceAccountAuthenticationService> m_AuthService = new();
    readonly Mock<IConfigurationValidator> m_Validator = new();
    Configuration m_Configuration = null!;
    IdProviderService m_Service = null!;

    [SetUp]
    public void SetUp()
    {
        m_Api.Reset();
        m_AuthService.Reset();
        m_Validator.Reset();
        m_Configuration = new Configuration();
        m_Api.Setup(api => api.Configuration).Returns(m_Configuration);
        m_AuthService.Setup(auth => auth.GetAccessTokenAsync(It.IsAny<CancellationToken>())).ReturnsAsync("service-account-basic-token");
        m_Service = new IdProviderService(m_Api.Object, m_AuthService.Object, m_Validator.Object);
    }

    static IDProviderV2 Provider(string type = "steampowered.com", bool disabled = false) =>
        JsonConvert.DeserializeObject<IDProviderV2>(
            "{\"configId\":\"" + k_ConfigId + "\",\"type\":\"" + type + "\",\"clientId\":\"480\",\"disabled\":" + (disabled ? "true" : "false")
            + ",\"hasClientSecret\":true,\"steamConfig\":{\"additionalAppIds\":[{\"appId\":\"481\",\"description\":\"Demo\"}]}}")!;

    void Throws(int status, string body) =>
        m_Api.Setup(api => api.IdProviderListAsync(k_Project, It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ApiException(status, "error", body));

    [Test]
    public async Task ListAsync_PassesTheTypeFilterAndSetsTheAccessToken()
    {
        m_Api.Setup(api => api.IdProviderListAsync(k_Project, "steampowered.com", It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ListIDProviderV2Response(new List<IDProviderV2> { Provider() }, 1));

        var providers = await m_Service.ListAsync(k_ProjectId, "steampowered.com");

        var provider = providers.Single();
        Assert.That(provider.ConfigId, Is.EqualTo(k_ConfigId));
        Assert.That(provider.SteamConfig!.AdditionalAppIds!.Single().AppId, Is.EqualTo("481"));
        Assert.That(provider.HasClientSecret, Is.True);
        Assert.That(m_Configuration.DefaultHeaders["Authorization"], Is.EqualTo("Basic service-account-basic-token"));
        m_Validator.Verify(validator => validator.ThrowExceptionIfConfigInvalid(It.IsAny<string>(), k_ProjectId), Times.Once);
    }

    [Test]
    public async Task CreateAsync_OmitsTheClientIdForAProviderWithoutOne()
    {
        CreateIDProviderV2Request? sent = null;
        m_Api.Setup(api => api.IdProviderCreateAsync(k_Project, It.IsAny<CreateIDProviderV2Request>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Callback<Guid, CreateIDProviderV2Request, int, CancellationToken>((_, request, _, _) => sent = request)
            .ReturnsAsync(Provider("usernamepassword"));

        await m_Service.CreateAsync(k_ProjectId, new CreateIdProviderRequest { Type = "usernamepassword" });

        Assert.That(JsonConvert.SerializeObject(sent), Is.EqualTo("{\"disabled\":false,\"type\":\"usernamepassword\"}"));
    }

    [Test]
    public async Task UpdateAsync_PatchesOnlyTheFieldsSet()
    {
        IDProviderV2Properties? sent = null;
        m_Api.Setup(api => api.IdProviderConfigUpdateAsync(k_Project, k_Config, It.IsAny<IDProviderV2Properties>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Callback<Guid, Guid, IDProviderV2Properties, int, CancellationToken>((_, _, request, _, _) => sent = request)
            .ReturnsAsync(Provider());

        await m_Service.UpdateAsync(k_ProjectId, k_ConfigId, new UpdateIdProviderRequest { ClientId = "new-client" });

        Assert.That(JsonConvert.SerializeObject(sent), Is.EqualTo("{\"clientId\":\"new-client\"}"));
    }

    [Test]
    public async Task UpdateAsync_ClearingAppIdsSendsAnEmptyList()
    {
        IDProviderV2Properties? sent = null;
        m_Api.Setup(api => api.IdProviderConfigUpdateAsync(k_Project, k_Config, It.IsAny<IDProviderV2Properties>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Callback<Guid, Guid, IDProviderV2Properties, int, CancellationToken>((_, _, request, _, _) => sent = request)
            .ReturnsAsync(Provider());

        await m_Service.UpdateAsync(k_ProjectId, k_ConfigId,
            new UpdateIdProviderRequest { SteamConfig = new SteamProviderConfig { AdditionalAppIds = new List<AdditionalAppId>() } });

        Assert.That(JsonConvert.SerializeObject(sent), Is.EqualTo("{\"steamConfig\":{\"additionalAppIds\":[]}}"));
    }

    [Test]
    public async Task StateChangesAndDelete_AddressTheConfigId()
    {
        m_Api.Setup(api => api.IdProviderDisableAsync(k_Project, k_Config, It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Provider(disabled: true));
        m_Api.Setup(api => api.IdProviderEnableAsync(k_Project, k_Config, It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Provider());

        Assert.That((await m_Service.DisableAsync(k_ProjectId, k_ConfigId)).Disabled, Is.True);
        Assert.That((await m_Service.EnableAsync(k_ProjectId, k_ConfigId)).Disabled, Is.False);
        await m_Service.DeleteAsync(k_ProjectId, k_ConfigId);

        m_Api.Verify(api => api.IdProviderDeleteConfigAsync(k_Project, k_Config, It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public void ConfigIdThatIsNotAGuid_IsRefusedBeforeAnyCall()
    {
        var exception = Assert.ThrowsAsync<CliException>(() => m_Service.GetAsync(k_ProjectId, "steam"));

        Assert.That(exception!.Message, Does.Contain("not a configuration ID"));
        m_Api.Verify(api => api.IdProviderConfigGetAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public void Unauthorized_ExplainsHowToLogIn()
    {
        Throws(401, "{\"title\":\"Unauthorized\",\"code\":51}");

        var exception = Assert.ThrowsAsync<CliException>(() => m_Service.ListAsync(k_ProjectId));

        Assert.That(exception!.Message, Does.Contain("--service-key-id"));
    }

    [Test]
    public void Forbidden_NamesThePermissionTheCallNeeds()
    {
        m_Api.Setup(api => api.IdProviderDeleteConfigAsync(k_Project, k_Config, It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ApiException(403, "error", "{\"title\":\"Forbidden\"}"));

        var exception = Assert.ThrowsAsync<CliException>(() => m_Service.DeleteAsync(k_ProjectId, k_ConfigId));

        Assert.That(exception!.Message, Does.Contain("player_auth.id_provider.delete"));
    }

    [Test]
    public void Failure_QuotesTheProblemButMasksTheSecretItEchoes()
    {
        m_Api.Setup(api => api.IdProviderConfigUpdateAsync(k_Project, k_Config, It.IsAny<IDProviderV2Properties>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ApiException(400, "error", "{\"title\":\"INVALID_PARAMETERS\",\"code\":42,\"detail\":\"bad key " + k_Secret + "\"}"));

        var exception = Assert.ThrowsAsync<CliException>(() => m_Service.UpdateAsync(k_ProjectId, k_ConfigId,
            new UpdateIdProviderRequest { ClientSecret = k_Secret }));

        Assert.That(exception!.Message, Does.Contain("INVALID_PARAMETERS (code 42): bad key ***"));
        Assert.That(exception.Message, Does.Not.Contain(k_Secret));
        Assert.That(exception.ExitCode, Is.EqualTo(ExitCode.HandledError));
    }

    [Test]
    public void Failure_MasksTheSecretWhereTheDetailQuotesTheRequestAsJson()
    {
        const string secret = "key\"with\\escapes";
        var echoedRequest = JsonConvert.SerializeObject(new { clientSecret = secret });
        var body = JsonConvert.SerializeObject(new { title = "INVALID_PARAMETERS", detail = "rejected " + echoedRequest });
        m_Api.Setup(api => api.IdProviderConfigUpdateAsync(k_Project, k_Config, It.IsAny<IDProviderV2Properties>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ApiException(400, "error", body));

        var exception = Assert.ThrowsAsync<CliException>(() => m_Service.UpdateAsync(k_ProjectId, k_ConfigId,
            new UpdateIdProviderRequest { ClientSecret = secret }));

        Assert.That(exception!.Message, Does.Contain("{\"clientSecret\":\"***\"}"));
        Assert.That(exception.Message, Does.Not.Contain("with"));
    }

    [Test]
    public void Failure_QuotesTheRequestIdForSupport()
    {
        Throws(400, "{\"title\":\"INVALID_PARAMETERS\",\"status\":400,\"detail\":\"invalid client id\",\"details\":[],\"requestId\":\"42065727-a7f9\"}");

        var exception = Assert.ThrowsAsync<CliException>(() => m_Service.ListAsync(k_ProjectId));

        Assert.That(exception!.Message, Does.EndWith("INVALID_PARAMETERS: invalid client id (request ID 42065727-a7f9)."));
    }

    [Test]
    public void Failure_KeepsTheRequestIdWhenTheProblemHasNoTitle()
    {
        Throws(500, "{\"status\":500,\"requestId\":\"0b5f-req\"}");

        var exception = Assert.ThrowsAsync<CliException>(() => m_Service.ListAsync(k_ProjectId));

        Assert.That(exception!.Message, Is.EqualTo("Failed to list the identity providers (HTTP 500). Request ID 0b5f-req."));
    }

    [TestCase(0)]
    [TestCase(200)]
    public void Failure_MasksTheSecretInTransportAndParseErrors(int status)
    {
        m_Api.Setup(api => api.IdProviderConfigUpdateAsync(k_Project, k_Config, It.IsAny<IDProviderV2Properties>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ApiException(status, "error", "Unexpected character near '" + k_Secret + "'"));

        var exception = Assert.ThrowsAsync<CliException>(() => m_Service.UpdateAsync(k_ProjectId, k_ConfigId,
            new UpdateIdProviderRequest { ClientSecret = k_Secret }));

        Assert.That(exception!.Message, Does.Contain("near '***'"));
        Assert.That(exception.Message, Does.Not.Contain(k_Secret));
    }

    [Test]
    public void Failure_ToleratesABodyThatIsNotJson()
    {
        Throws(502, "<html>bad gateway</html>");

        var exception = Assert.ThrowsAsync<CliException>(() => m_Service.ListAsync(k_ProjectId));

        Assert.That(exception!.Message, Does.Contain("HTTP 502"));
    }

    [Test]
    public void Failure_MasksTheSecretInTheTitleToo()
    {
        m_Api.Setup(api => api.IdProviderConfigUpdateAsync(k_Project, k_Config, It.IsAny<IDProviderV2Properties>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ApiException(400, "error", "{\"title\":\"bad key " + k_Secret + "\"}"));

        var exception = Assert.ThrowsAsync<CliException>(() => m_Service.UpdateAsync(k_ProjectId, k_ConfigId,
            new UpdateIdProviderRequest { ClientSecret = k_Secret }));

        Assert.That(exception!.Message, Does.Contain("bad key ***"));
        Assert.That(exception.Message, Does.Not.Contain(k_Secret));
    }

    [Test]
    public void Constructor_InstallsTheTransportAwareExceptionFactory()
    {
        m_Api.VerifySet(api => api.ExceptionFactory = IdProviderService.RaiseForUnusableResponses);
    }

    [Test]
    public void ExceptionFactory_RaisesForAResponseThatNeverArrived()
    {
        var response = new Mock<IApiResponse>();
        response.Setup(r => r.StatusCode).Returns((HttpStatusCode)0);
        response.Setup(r => r.ErrorText).Returns("No such host is known.");

        var exception = IdProviderService.RaiseForUnusableResponses("IdProviderDeleteConfig", response.Object) as ApiException;

        Assert.That(exception, Is.Not.Null);
        Assert.That(exception!.ErrorCode, Is.EqualTo(0));
        Assert.That(exception.ErrorContent, Is.EqualTo("No such host is known."));
    }

    static IApiResponse Response(int status, string? contentType, string? body, string? errorText = null)
    {
        var headers = new Multimap<string, string>();
        if (contentType is not null)
        {
            headers.Add("Content-Type", contentType);
        }

        var response = new Mock<IApiResponse>();
        response.Setup(r => r.StatusCode).Returns((HttpStatusCode)status);
        response.Setup(r => r.Headers).Returns(headers);
        response.Setup(r => r.RawContent).Returns(body!);
        response.Setup(r => r.ErrorText).Returns(errorText!);
        return response.Object;
    }

    [Test]
    public void ExceptionFactory_RaisesForA200WebPage()
    {
        var exception = IdProviderService.RaiseForUnusableResponses(
            "IdProviderDeleteConfig", Response(200, "text/html; charset=utf-8", "<html>Wi-Fi login</html>")) as ApiException;

        Assert.That(exception!.ErrorCode, Is.EqualTo(200));
        Assert.That(exception.ErrorContent, Is.EqualTo("the response was text/html; charset=utf-8, not JSON"));
    }

    [Test]
    public void ExceptionFactory_RaisesForA200ThatDidNotParse()
    {
        var exception = IdProviderService.RaiseForUnusableResponses(
            "IdProviderList", Response(200, null, "<html/>", "Unexpected character encountered while parsing value: <.")) as ApiException;

        Assert.That(exception!.ErrorCode, Is.EqualTo(200));
        Assert.That(exception.ErrorContent, Does.StartWith("Unexpected character"));
    }

    [TestCase("application/json; charset=utf-8", "{\"results\":[],\"total\":0}")]
    [TestCase("application/problem+json", "{}")]
    [TestCase(null, "")]
    [TestCase("text/plain", "")]
    public void ExceptionFactory_PassesAUsable2xxThrough(string? contentType, string body)
    {
        Assert.That(IdProviderService.RaiseForUnusableResponses("IdProviderList", Response(200, contentType, body)), Is.Null);
    }

    [Test]
    public void ExceptionFactory_LeavesA4xxToTheDefaultFactory()
    {
        const string problem = "{\"title\":\"NOT_FOUND\",\"status\":404}";

        var exception = IdProviderService.RaiseForUnusableResponses(
            "IdProviderConfigGet", Response(404, "application/problem+json", problem)) as ApiException;

        Assert.That(exception!.ErrorCode, Is.EqualTo(404));
        Assert.That(exception.ErrorContent, Is.EqualTo(problem));
    }

    [Test]
    public void UnreadableAnswer_OnDeleteReportsItInsteadOfSuccess()
    {
        m_Api.Setup(api => api.IdProviderDeleteConfigAsync(k_Project, k_Config, It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ApiException(200, "Error calling IdProviderDeleteConfig", "the response was text/html, not JSON"));

        var exception = Assert.ThrowsAsync<CliException>(() => m_Service.DeleteAsync(k_ProjectId, k_ConfigId));

        Assert.That(exception!.Message, Is.EqualTo(
            $"Could not read the answer when trying to delete configuration {k_ConfigId}: the response was text/html, not JSON. "
            + "A proxy or network sign-in page may be answering instead of the services gateway. Run "
            + "`ugs player id-provider list` to check whether the change was applied."));
    }

    [Test]
    public void UnreadableAnswer_OnListGivesNoChangeHintAndNoDoubleFullStop()
    {
        Throws(200, "Unexpected character encountered while parsing value: <. Path '', line 0, position 0.");

        var exception = Assert.ThrowsAsync<CliException>(() => m_Service.ListAsync(k_ProjectId));

        Assert.That(exception!.Message, Is.EqualTo(
            "Could not read the answer when trying to list the identity providers: Unexpected character encountered while "
            + "parsing value: <. Path '', line 0, position 0. A proxy or network sign-in page may be answering instead of "
            + "the services gateway."));
    }

    [Test]
    public void NoData_IsAFailureNotAnEmptyList()
    {
        m_Api.Setup(api => api.IdProviderListAsync(k_Project, It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ListIDProviderV2Response)null!);

        var exception = Assert.ThrowsAsync<CliException>(() => m_Service.ListAsync(k_ProjectId));

        Assert.That(exception!.Message, Is.EqualTo("The services gateway returned no data when trying to list the identity providers."));
    }

    [Test]
    public void TransportFailure_OnDeleteReportsTheCauseInsteadOfSuccess()
    {
        m_Api.Setup(api => api.IdProviderDeleteConfigAsync(k_Project, k_Config, It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ApiException(0, "Error calling IdProviderDeleteConfig: No such host is known.", "No such host is known."));

        var exception = Assert.ThrowsAsync<CliException>(() => m_Service.DeleteAsync(k_ProjectId, k_ConfigId));

        Assert.That(exception!.Message, Is.EqualTo(
            $"Could not reach the services gateway to delete configuration {k_ConfigId}: No such host is known."));
    }
}
