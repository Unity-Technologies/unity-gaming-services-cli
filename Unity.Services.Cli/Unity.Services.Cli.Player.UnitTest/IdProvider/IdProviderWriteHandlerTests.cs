using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json;
using NUnit.Framework;
using Spectre.Console;
using Unity.Services.Cli.Common.Console;
using Unity.Services.Cli.Common.Exceptions;
using Unity.Services.Cli.Common.Logging;
using Unity.Services.Cli.Player.IdProvider.Handlers;
using Unity.Services.Cli.Player.IdProvider.Input;
using Unity.Services.Cli.Player.IdProvider.Models;
using Unity.Services.Cli.Player.IdProvider.Service;

namespace Unity.Services.Cli.Player.UnitTest.IdProvider;

public class IdProviderWriteHandlerTests
{
    const string k_ProjectId = "abcd1234-ab12-cd34-ef56-abcdef123456";
    const string k_SteamId = "8bdacc33-6eef-4577-beb0-633c86259f5b";
    const string k_OidcId = "0d1c0000-0000-4000-8000-0000000000d1";
    const string k_NewSecret = "rotated-steam-key";

    readonly Mock<IIdProviderService> m_Service = new();
    readonly Mock<IClientSecretInput> m_Stdin = new();
    readonly Mock<ILogger> m_Logger = new();
    readonly Mock<ILoadingIndicator> m_Loading = new();
    readonly List<object> m_Results = new();
    UpdateIdProviderRequest? m_SentUpdate;
    CreateIdProviderRequest? m_SentCreate;

    [SetUp]
    public void SetUp()
    {
        m_Service.Reset();
        m_Stdin.Reset();
        m_Logger.Reset();
        m_Loading.Reset();
        m_Results.Clear();
        m_SentUpdate = null;
        m_SentCreate = null;
        m_Logger
            .Setup(logger => logger.Log(
                LogLevel.Critical,
                LoggerExtension.ResultEventId,
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()))
            .Callback(new InvocationAction(invocation => m_Results.Add(invocation.Arguments[2])));
        m_Loading
            .Setup(loading => loading.StartLoadingAsync(It.IsAny<string>(), It.IsAny<Func<StatusContext?, Task>>()))
            .Returns<string, Func<StatusContext?, Task>>((_, work) => work(null));
        m_Stdin.Setup(stdin => stdin.IsRedirected).Returns(true);
        m_Stdin.Setup(stdin => stdin.ReadLineAsync(It.IsAny<CancellationToken>())).ReturnsAsync(k_NewSecret);
        m_Service
            .Setup(service => service.ListAsync(k_ProjectId, "steampowered.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<IdProviderResponse>
            {
                new()
                {
                    ConfigId = k_SteamId,
                    Type = "steampowered.com",
                    ClientId = "480",
                    SteamConfig = new SteamProviderConfig
                    {
                        AdditionalAppIds = new List<AdditionalAppId> { new() { AppId = "481", Description = "Demo" } },
                    },
                },
            });
        m_Service
            .Setup(service => service.GetAsync(k_ProjectId, k_OidcId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IdProviderResponse { ConfigId = k_OidcId, Type = "oidc-mygame", ClientId = "client" });
        m_Service
            .Setup(service => service.UpdateAsync(k_ProjectId, It.IsAny<string>(), It.IsAny<UpdateIdProviderRequest>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, UpdateIdProviderRequest, CancellationToken>((_, _, request, _) => m_SentUpdate = request)
            .ReturnsAsync(new IdProviderResponse { ConfigId = k_SteamId, Type = "steampowered.com" });
        m_Service
            .Setup(service => service.CreateAsync(k_ProjectId, It.IsAny<CreateIdProviderRequest>(), It.IsAny<CancellationToken>()))
            .Callback<string, CreateIdProviderRequest, CancellationToken>((_, request, _) => m_SentCreate = request)
            .ReturnsAsync(new IdProviderResponse { ConfigId = k_SteamId, Type = "steampowered.com" });
    }

    Task Create(IdProviderInput input) => IdProviderCreateHandler.CreateAsync(
        input, m_Service.Object, m_Stdin.Object, m_Logger.Object, m_Loading.Object, CancellationToken.None);

    Task Update(IdProviderInput input) => IdProviderUpdateHandler.UpdateAsync(
        input, m_Service.Object, m_Stdin.Object, m_Logger.Object, m_Loading.Object, CancellationToken.None);

    [Test]
    public async Task Create_SendsTheSecretFromStdinAndTheSteamAppIds()
    {
        await Create(new IdProviderInput
        {
            CloudProjectId = k_ProjectId,
            Provider = "steam",
            ClientId = "480",
            ClientSecretFromStdin = true,
            AdditionalAppIds = new List<string> { "481=Demo", "482=Extra" },
        });

        Assert.That(m_SentCreate!.ClientSecret, Is.EqualTo(k_NewSecret));
        Assert.That(m_SentCreate.SteamConfig!.AdditionalAppIds!.Select(id => id.AppId), Is.EqualTo(new[] { "481", "482" }));
        Assert.That(JsonConvert.SerializeObject(m_SentCreate), Does.Not.Contain("\"oidcConfig\""));
        Assert.That(JsonConvert.SerializeObject(m_Results.Single()), Does.Not.Contain(k_NewSecret));
    }

    [Test]
    public void Create_RequiresTheSecretAProviderNeeds()
    {
        var exception = Assert.ThrowsAsync<CliException>(() => Create(new IdProviderInput
        {
            CloudProjectId = k_ProjectId,
            Provider = "steam",
            ClientId = "480",
        }));

        Assert.That(exception!.Message, Does.Contain(IdProviderInput.ClientSecretStdinAlias));
        m_Service.VerifyNoOtherCalls();
    }

    [Test]
    public void Create_RefusesUnityAndPointsToPlayerAccounts()
    {
        var exception = Assert.ThrowsAsync<CliException>(() => Create(new IdProviderInput
        {
            CloudProjectId = k_ProjectId,
            Provider = "unity",
            ClientId = "typed-in",
        }));

        Assert.That(exception!.Message, Does.StartWith("'unity' cannot be created directly."));
        Assert.That(exception.Message, Does.Contain("Unity Player Accounts"));
        m_Service.VerifyNoOtherCalls();
    }

    [Test]
    public async Task Update_DryRunPreviewsARepointOfUnityWithAWarning()
    {
        StoredUnity();

        await Update(new IdProviderInput { CloudProjectId = k_ProjectId, Config = "unity", ClientId = "other", DryRun = true });

        VerifyPlayerAccountsWarning(Times.Once());
        m_Service.Verify(service => service.UpdateAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<UpdateIdProviderRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    const string k_UnityId = "29516268-859c-5ef6-b60b-8300be4d9fb4";

    void StoredUnity() => m_Service
        .Setup(service => service.ListAsync(k_ProjectId, "unity", It.IsAny<CancellationToken>()))
        .ReturnsAsync(new List<IdProviderResponse> { new() { ConfigId = k_UnityId, Type = "unity", ClientId = "client" } });

    void VerifyPlayerAccountsWarning(Times times) => m_Logger.Verify(logger => logger.Log(
        LogLevel.Warning,
        It.IsAny<EventId>(),
        It.Is<It.IsAnyType>((state, _) => state.ToString()!.Contains("points Player Accounts sign-in away")),
        It.IsAny<Exception?>(),
        It.IsAny<Func<It.IsAnyType, Exception?, string>>()), times);

    [Test]
    public void Update_RefusesToRepointUnityWithoutForce()
    {
        StoredUnity();

        var exception = Assert.ThrowsAsync<CliException>(() =>
            Update(new IdProviderInput { CloudProjectId = k_ProjectId, Config = "unity", ClientId = "other" }));

        Assert.That(exception!.Message, Does.Contain("Pass --force to change it, or --dry-run to preview."));
        m_Service.Verify(service => service.UpdateAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<UpdateIdProviderRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task Update_RepointsUnityWithForceAndWarns()
    {
        StoredUnity();

        await Update(new IdProviderInput { CloudProjectId = k_ProjectId, Config = "unity", ClientId = "other", UseForce = true });

        Assert.That(m_SentUpdate!.ClientId, Is.EqualTo("other"));
        VerifyPlayerAccountsWarning(Times.Once());
    }

    [TestCase("client", null)]
    [TestCase(null, "https://relying.example")]
    public async Task Update_LeavesUnityAloneWhenTheClientIdDoesNotChange(string? clientId, string? relyingParty)
    {
        StoredUnity();

        await Update(new IdProviderInput { CloudProjectId = k_ProjectId, Config = "unity", ClientId = clientId, RelyingParty = relyingParty });

        m_Service.Verify(service => service.UpdateAsync(k_ProjectId, k_UnityId, It.IsAny<UpdateIdProviderRequest>(), It.IsAny<CancellationToken>()), Times.Once);
        VerifyPlayerAccountsWarning(Times.Never());
    }

    [Test]
    public void Create_RefusesASecretFromATerminal()
    {
        m_Stdin.Setup(stdin => stdin.IsRedirected).Returns(false);

        var exception = Assert.ThrowsAsync<CliException>(() => Create(new IdProviderInput
        {
            CloudProjectId = k_ProjectId,
            Provider = "steam",
            ClientId = "480",
            ClientSecretFromStdin = true,
        }));

        Assert.That(exception!.Message, Does.Contain("terminal"));
        Assert.That(exception.Message, Does.Contain("ugs player id-provider create"));
        m_Stdin.Verify(stdin => stdin.ReadLineAsync(It.IsAny<CancellationToken>()), Times.Never);
        m_Service.VerifyNoOtherCalls();
    }

    [Test]
    public void Update_NamesUpdateWhenTheSecretComesFromATerminal()
    {
        m_Stdin.Setup(stdin => stdin.IsRedirected).Returns(false);

        var exception = Assert.ThrowsAsync<CliException>(() => Update(new IdProviderInput
        {
            CloudProjectId = k_ProjectId,
            Config = "steam",
            ClientSecretFromStdin = true,
        }));

        Assert.That(exception!.Message, Does.Contain("ugs player id-provider update"));
    }

    [Test]
    public async Task Create_SendsTheDisplayNameForOpenIdConnect()
    {
        await Create(new IdProviderInput
        {
            CloudProjectId = k_ProjectId,
            Provider = "oidc-mygame",
            ClientId = "client",
            Issuer = "https://issuer.example",
            DisplayName = "My Game",
        });

        Assert.That(m_SentCreate!.DisplayName, Is.EqualTo("My Game"));
        Assert.That(m_SentCreate.OidcConfig!.Issuer, Is.EqualTo("https://issuer.example"));
    }

    [Test]
    public async Task Create_SendsTheDisplayNameForCustomId()
    {
        await Create(new IdProviderInput
        {
            CloudProjectId = k_ProjectId,
            Provider = "custom",
            ClientId = "client",
            ClientSecretFromStdin = true,
            DisplayName = "My Studio",
        });

        Assert.That(m_SentCreate!.DisplayName, Is.EqualTo("My Studio"));
    }

    [TestCase("google", "https://issuer.example", null, "--issuer applies to OpenID Connect providers only")]
    [TestCase("xbox", "https://issuer.example", null, "--issuer applies to OpenID Connect providers only")]
    [TestCase("apple", null, "481=Demo", "'apple.com' does not take --additional-app-id")]
    [TestCase("xbox", null, "481=Demo", "'xbox' does not take --additional-app-id")]
    public void Create_RefusesFieldsTheApiWouldDropOrStoreOnTheWrongType(string provider, string? issuer, string? appId, string expected)
    {
        var exception = Assert.ThrowsAsync<CliException>(() => Create(new IdProviderInput
        {
            CloudProjectId = k_ProjectId,
            Provider = provider,
            ClientId = "client",
            Issuer = issuer,
            AdditionalAppIds = appId is null ? null : new List<string> { appId },
        }));

        Assert.That(exception!.Message, Does.Contain(expected));
        m_Service.VerifyNoOtherCalls();
    }

    [Test]
    public async Task Create_LeavesTheClientIdIssuerAndDisplayNameRulesToTheApi()
    {
        await Create(new IdProviderInput { CloudProjectId = k_ProjectId, Provider = "oidc-mygame", DisplayName = "Name" });

        Assert.That(JsonConvert.SerializeObject(m_SentCreate), Is.EqualTo("{\"type\":\"oidc-mygame\",\"displayName\":\"Name\"}"));
    }

    [Test]
    public async Task Create_SendsATypeThisCliDoesNotKnowAndWarns()
    {
        await Create(new IdProviderInput { CloudProjectId = k_ProjectId, Provider = "xbox", ClientId = "title-id" });

        Assert.That(m_SentCreate!.Type, Is.EqualTo("xbox"));
        Assert.That(m_SentCreate.ClientId, Is.EqualTo("title-id"));
        m_Logger.Verify(logger => logger.Log(
            LogLevel.Warning,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((state, _) => state.ToString()!.Contains("'xbox' is not a provider type this CLI knows")),
            It.IsAny<Exception?>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }

    [TestCase("482")]
    [TestCase("482=")]
    [TestCase("482=demo app")]
    [TestCase("482=via-update")]
    public void Create_RefusesAnAppIdTheApiWouldReject(string entry)
    {
        var exception = Assert.ThrowsAsync<CliException>(() => Create(new IdProviderInput
        {
            CloudProjectId = k_ProjectId,
            Provider = "steam",
            ClientId = "480",
            ClientSecretFromStdin = true,
            AdditionalAppIds = new List<string> { entry },
        }));

        Assert.That(exception!.Message, Does.Contain("appId=Description"));
        m_Service.VerifyNoOtherCalls();
    }

    [Test]
    public async Task Update_PatchesOnlyThePassedFieldsWithoutReadingASecret()
    {
        await Update(new IdProviderInput { CloudProjectId = k_ProjectId, Config = "steam", ClientId = "999" });

        Assert.That(JsonConvert.SerializeObject(m_SentUpdate), Is.EqualTo("{\"clientId\":\"999\"}"));
        var result = (IdProviderChangeResult)m_Results.Single();
        Assert.That(result.Message, Is.EqualTo($"Wrote client-id on configuration {k_SteamId} ('steampowered.com')."));
        m_Service.Verify(service => service.UpdateAsync(k_ProjectId, k_SteamId, It.IsAny<UpdateIdProviderRequest>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task Update_DryRunNamesTheChangedFieldsWithoutWriting()
    {
        await Update(new IdProviderInput
        {
            CloudProjectId = k_ProjectId,
            Config = "steam",
            ClientSecretFromStdin = true,
            ClearAdditionalAppIds = true,
            DryRun = true,
        });

        var result = (IdProviderChangeResult)m_Results.Single();
        Assert.That(result.Status, Is.EqualTo(IdProviderChangeResult.DryRun));
        Assert.That(result.Message, Does.StartWith("Would write client-secret, additional-app-ids"));
        Assert.That(JsonConvert.SerializeObject(result), Does.Not.Contain(k_NewSecret));
        m_Service.Verify(service => service.UpdateAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<UpdateIdProviderRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task Update_ClearingAppIdsSendsAnEmptyList()
    {
        await Update(new IdProviderInput { CloudProjectId = k_ProjectId, Config = "steam", ClearAdditionalAppIds = true });

        Assert.That(JsonConvert.SerializeObject(m_SentUpdate), Is.EqualTo("{\"steamConfig\":{\"additionalAppIds\":[]}}"));
    }

    [Test]
    public void Update_ValidatesAgainstTheStoredConfigurationsType()
    {
        var exception = Assert.ThrowsAsync<CliException>(() => Update(new IdProviderInput
        {
            CloudProjectId = k_ProjectId,
            Config = k_OidcId,
            AdditionalAppIds = new List<string> { "1=Demo" },
        }));

        Assert.That(exception!.Message, Does.Contain("does not take --additional-app-id"));
    }

    [Test]
    public void Update_RefusesToClearAppIdsOnATypeWithoutThem()
    {
        var exception = Assert.ThrowsAsync<CliException>(() => Update(new IdProviderInput
        {
            CloudProjectId = k_ProjectId,
            Config = k_OidcId,
            ClearAdditionalAppIds = true,
            DryRun = true,
        }));

        Assert.That(exception!.Message, Does.Contain("does not take --clear-additional-app-ids"));
        m_Service.Verify(service => service.UpdateAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<UpdateIdProviderRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public void Update_RefusesWhenNothingIsPassed()
    {
        var exception = Assert.ThrowsAsync<CliException>(() => Update(new IdProviderInput
        {
            CloudProjectId = k_ProjectId,
            Config = "steam",
        }));

        Assert.That(exception!.Message, Does.Contain("Nothing to update"));
        m_Service.VerifyNoOtherCalls();
    }
}
