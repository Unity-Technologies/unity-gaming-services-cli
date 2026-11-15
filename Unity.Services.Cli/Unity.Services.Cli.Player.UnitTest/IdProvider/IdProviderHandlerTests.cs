using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using Unity.Services.Cli.Common.Exceptions;
using Unity.Services.Cli.Common.Logging;
using Unity.Services.Cli.Player.IdProvider.Handlers;
using Unity.Services.Cli.Player.IdProvider.Input;
using Unity.Services.Cli.Player.IdProvider.Models;
using Unity.Services.Cli.Player.IdProvider.Service;

namespace Unity.Services.Cli.Player.UnitTest.IdProvider;

public class IdProviderHandlerTests
{
    const string k_ProjectId = "abcd1234-ab12-cd34-ef56-abcdef123456";
    const string k_SteamId = "8bdacc33-6eef-4577-beb0-633c86259f5b";
    const string k_GoogleA = "41fe0c2e-0000-4000-8000-00000000000a";
    const string k_GoogleB = "41fe0c2e-0000-4000-8000-00000000000b";
    const string k_PasswordId = "77777777-0000-4000-8000-000000000007";
    const string k_UnityId = "29516268-859c-5ef6-b60b-8300be4d9fb4";

    readonly Mock<IIdProviderService> m_Service = new();
    readonly Mock<ILogger> m_Logger = new();
    readonly List<object> m_Results = new();

    static readonly IdProviderResponse k_Steam = new() { ConfigId = k_SteamId, Type = "steampowered.com", ClientId = "480" };

    [SetUp]
    public void SetUp()
    {
        m_Service.Reset();
        m_Logger.Reset();
        m_Results.Clear();
        m_Logger
            .Setup(logger => logger.Log(
                LogLevel.Critical,
                LoggerExtension.ResultEventId,
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()))
            .Callback(new InvocationAction(invocation => m_Results.Add(invocation.Arguments[2])));
        m_Service
            .Setup(service => service.ListAsync(k_ProjectId, "steampowered.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<IdProviderResponse> { k_Steam });
        m_Service
            .Setup(service => service.ListAsync(k_ProjectId, "google.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<IdProviderResponse>
            {
                new() { ConfigId = k_GoogleA, Type = "google.com" },
                new() { ConfigId = k_GoogleB, Type = "google.com" },
            });
        m_Service
            .Setup(service => service.ListAsync(k_ProjectId, "apple.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<IdProviderResponse>());
        m_Service
            .Setup(service => service.GetAsync(k_ProjectId, k_SteamId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(k_Steam);
        m_Service
            .Setup(service => service.ListAsync(k_ProjectId, "unity", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<IdProviderResponse> { new() { ConfigId = k_UnityId, Type = "unity", ClientId = "client" } });
        m_Service
            .Setup(service => service.GetAsync(k_ProjectId, k_PasswordId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IdProviderResponse { ConfigId = k_PasswordId, Type = "usernamepassword" });
    }

    static IdProviderInput Input(string config, bool force = false, bool dryRun = false) => new()
    {
        CloudProjectId = k_ProjectId,
        Config = config,
        UseForce = force,
        DryRun = dryRun,
    };

    [Test]
    public async Task Get_ResolvesAUniqueTypeToItsConfiguration()
    {
        await IdProviderGetHandler.GetAsync(Input("Steam"), m_Service.Object, m_Logger.Object, CancellationToken.None);

        var result = (IdProviderResult)m_Results.Single();
        Assert.That(result.ConfigId, Is.EqualTo(k_SteamId));
        Assert.That(result.Name, Is.EqualTo("Steam"));
        m_Service.Verify(service => service.GetAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task Get_FetchesAConfigIdDirectly()
    {
        await IdProviderGetHandler.GetAsync(Input(k_SteamId), m_Service.Object, m_Logger.Object, CancellationToken.None);

        m_Service.Verify(service => service.GetAsync(k_ProjectId, k_SteamId, It.IsAny<CancellationToken>()), Times.Once);
        m_Service.Verify(service => service.ListAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public void Get_RefusesATypeWithSeveralConfigurationsAndListsThem()
    {
        var exception = Assert.ThrowsAsync<CliException>(() =>
            IdProviderGetHandler.GetAsync(Input("google"), m_Service.Object, m_Logger.Object, CancellationToken.None));

        Assert.That(exception!.Message, Does.Contain(k_GoogleA));
        Assert.That(exception.Message, Does.Contain(k_GoogleB));
    }

    [Test]
    public void Get_RefusesATypeWithNoConfiguration()
    {
        var exception = Assert.ThrowsAsync<CliException>(() =>
            IdProviderGetHandler.GetAsync(Input("apple"), m_Service.Object, m_Logger.Object, CancellationToken.None));

        Assert.That(exception!.Message, Does.Contain("No 'apple.com' configuration"));
    }

    [Test]
    public void Delete_RefusesWithoutForce()
    {
        var exception = Assert.ThrowsAsync<CliException>(() =>
            IdProviderDeleteHandler.DeleteAsync(Input("steam"), m_Service.Object, m_Logger.Object, CancellationToken.None));

        Assert.That(exception!.Message, Does.Contain("--force"));
        m_Service.Verify(service => service.DeleteAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task Delete_DryRunReportsWithoutDeleting()
    {
        await IdProviderDeleteHandler.DeleteAsync(Input("steam", dryRun: true), m_Service.Object, m_Logger.Object, CancellationToken.None);

        var result = (IdProviderChangeResult)m_Results.Single();
        Assert.That(result.Status, Is.EqualTo(IdProviderChangeResult.DryRun));
        Assert.That(result.ConfigId, Is.EqualTo(k_SteamId));
        m_Service.Verify(service => service.DeleteAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task Delete_WithForceDeletesTheResolvedConfiguration()
    {
        await IdProviderDeleteHandler.DeleteAsync(Input("Steam", force: true), m_Service.Object, m_Logger.Object, CancellationToken.None);

        m_Service.Verify(service => service.DeleteAsync(k_ProjectId, k_SteamId, It.IsAny<CancellationToken>()), Times.Once);
        Assert.That(((IdProviderChangeResult)m_Results.Single()).Status, Is.EqualTo(IdProviderChangeResult.Applied));
    }

    [Test]
    public void Delete_RefusesAnUndeletableType()
    {
        Assert.ThrowsAsync<CliException>(() =>
            IdProviderDeleteHandler.DeleteAsync(Input(k_PasswordId, force: true), m_Service.Object, m_Logger.Object, CancellationToken.None));

        m_Service.Verify(service => service.DeleteAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public void Get_RefusesUnityWhenSeveralConfigurationsHoldIt()
    {
        const string secondUnity = "29516268-0000-4000-8000-000000000002";
        m_Service
            .Setup(service => service.ListAsync(k_ProjectId, "unity", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<IdProviderResponse>
            {
                new() { ConfigId = k_UnityId, Type = "unity" },
                new() { ConfigId = secondUnity, Type = "unity" },
            });

        var exception = Assert.ThrowsAsync<CliException>(() =>
            IdProviderGetHandler.GetAsync(Input("unity"), m_Service.Object, m_Logger.Object, CancellationToken.None));

        Assert.That(exception!.Message, Does.Contain(k_UnityId));
        Assert.That(exception.Message, Does.Contain(secondUnity));
    }

    [Test]
    public async Task Delete_WarnsThatThePlayerAccountsBackendMaintainsUnity()
    {
        await IdProviderDeleteHandler.DeleteAsync(Input("unity", dryRun: true), m_Service.Object, m_Logger.Object, CancellationToken.None);

        m_Logger.Verify(logger => logger.Log(
            LogLevel.Warning,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((state, _) => state.ToString()!.Contains("turns off Unity Player Accounts sign-in")),
            It.IsAny<Exception?>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
        Assert.That(((IdProviderChangeResult)m_Results.Single()).Status, Is.EqualTo(IdProviderChangeResult.DryRun));
    }

    [Test]
    public async Task Delete_DoesNotWarnForOtherTypes()
    {
        await IdProviderDeleteHandler.DeleteAsync(Input("steam", dryRun: true), m_Service.Object, m_Logger.Object, CancellationToken.None);

        m_Logger.Verify(logger => logger.Log(
            LogLevel.Warning,
            It.IsAny<EventId>(),
            It.IsAny<It.IsAnyType>(),
            It.IsAny<Exception?>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Never);
    }

    [Test]
    public async Task Enable_ReportsTheConfigurationAfterTheChange()
    {
        m_Service
            .Setup(service => service.EnableAsync(k_ProjectId, k_SteamId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IdProviderResponse { ConfigId = k_SteamId, Type = "steampowered.com", Disabled = false });

        await IdProviderEnableHandler.EnableAsync(Input("steam"), m_Service.Object, m_Logger.Object, CancellationToken.None);

        m_Service.Verify(service => service.EnableAsync(k_ProjectId, k_SteamId, It.IsAny<CancellationToken>()), Times.Once);
        Assert.That(((IdProviderResult)m_Results.Single()).Enabled, Is.True);
    }

    [Test]
    public async Task Disable_ReportsTheConfigurationAfterTheChange()
    {
        m_Service
            .Setup(service => service.DisableAsync(k_ProjectId, k_SteamId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IdProviderResponse { ConfigId = k_SteamId, Type = "steampowered.com", Disabled = true });

        await IdProviderDisableHandler.DisableAsync(Input("steam"), m_Service.Object, m_Logger.Object, CancellationToken.None);

        Assert.That(((IdProviderResult)m_Results.Single()).Enabled, Is.False);
    }

    [Test]
    public void Result_ReportsEmptyOptionalFieldsAsNull()
    {
        var result = IdProviderResult.From(new IdProviderResponse
        {
            ConfigId = k_PasswordId,
            Type = "usernamepassword",
            ClientId = "",
            RelyingParty = "",
            DisplayName = "",
            OidcConfig = new OpenIdConfig { Issuer = "" },
        });

        var json = JObject.Parse(JsonConvert.SerializeObject(result));
        foreach (var field in new[] { "clientId", "relyingParty", "displayName", "issuer" })
        {
            Assert.That(json[field]!.Type, Is.EqualTo(JTokenType.Null), field);
        }
    }

    [Test]
    public async Task List_WarnsWhenTheTypeFilterIsNotOneThisCliKnows()
    {
        m_Service
            .Setup(service => service.ListAsync(k_ProjectId, "stema", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<IdProviderResponse>());

        await IdProviderListHandler.ListAsync(new IdProviderInput { CloudProjectId = k_ProjectId, TypeFilter = "stema" },
            m_Service.Object, m_Logger.Object, CancellationToken.None);

        m_Logger.Verify(logger => logger.Log(
            LogLevel.Warning,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((state, _) => state.ToString()!.Contains("'stema' is not a provider type this CLI knows")),
            It.IsAny<Exception?>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }

    [Test]
    public void Get_PointsToListTypesWhenAnUnknownTypeMatchesNothing()
    {
        m_Service
            .Setup(service => service.ListAsync(k_ProjectId, "gogle", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<IdProviderResponse>());

        var exception = Assert.ThrowsAsync<CliException>(() =>
            IdProviderGetHandler.GetAsync(Input("gogle"), m_Service.Object, m_Logger.Object, CancellationToken.None));

        Assert.That(exception!.Message, Does.EndWith("'gogle' is not a type this CLI knows, see `ugs player id-provider list-types`."));
    }

    [Test]
    public async Task List_PassesTheResolvedTypeFilter()
    {
        await IdProviderListHandler.ListAsync(new IdProviderInput { CloudProjectId = k_ProjectId, TypeFilter = "Steam" },
            m_Service.Object, m_Logger.Object, CancellationToken.None);

        m_Service.Verify(service => service.ListAsync(k_ProjectId, "steampowered.com", It.IsAny<CancellationToken>()), Times.Once);
        Assert.That(JsonConvert.SerializeObject(m_Results.Single()), Does.Not.Contain("clientSecret"));
        VerifyNoWarning();
    }

    [Test]
    public async Task List_DoesNotWarnForAnOpenIdConnectTypeFilter()
    {
        m_Service
            .Setup(service => service.ListAsync(k_ProjectId, "oidc-mygame", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<IdProviderResponse>());

        await IdProviderListHandler.ListAsync(new IdProviderInput { CloudProjectId = k_ProjectId, TypeFilter = "oidc-mygame" },
            m_Service.Object, m_Logger.Object, CancellationToken.None);

        m_Service.Verify(service => service.ListAsync(k_ProjectId, "oidc-mygame", It.IsAny<CancellationToken>()), Times.Once);
        VerifyNoWarning();
    }

    void VerifyNoWarning() => m_Logger.Verify(logger => logger.Log(
        LogLevel.Warning,
        It.IsAny<EventId>(),
        It.IsAny<It.IsAnyType>(),
        It.IsAny<Exception?>(),
        It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Never);
}
