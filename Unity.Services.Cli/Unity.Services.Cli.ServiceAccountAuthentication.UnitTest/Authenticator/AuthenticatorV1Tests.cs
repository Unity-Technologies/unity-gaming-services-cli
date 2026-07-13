using System.Text;
using Moq;
using NUnit.Framework;
using Spectre.Console;
using Unity.Services.Cli.Common.Console;
using Unity.Services.Cli.Common.Persister;
using Unity.Services.Cli.Common.SystemEnvironment;
using Unity.Services.Cli.ServiceAccountAuthentication;
using Unity.Services.Cli.ServiceAccountAuthentication.Exceptions;
using Unity.Services.Cli.ServiceAccountAuthentication.Hub;
using Unity.Services.Cli.ServiceAccountAuthentication.Input;
using Unity.Services.Cli.ServiceAccountAuthentication.Token;

namespace Unity.Services.Cli.Authentication.UnitTest;

[TestFixture]
class AuthenticatorV1Tests
{
    const string k_ValidServiceKeyId = "0e250400-c34a-4600-ac4b-f058b0d86b76";
    const string k_ValidServiceSecretKey = "apddVS3FPsTeN1hI_zBuHmHPF9WT2KAi";
    static readonly string k_AccessToken = Convert.ToBase64String(
        Encoding.UTF8.GetBytes($"{k_ValidServiceKeyId}:{k_ValidServiceSecretKey}"));

    readonly Mock<IConsolePrompt> m_MockPrompt = new();
    readonly Mock<IPersister<string>> m_MockPersister = new();

    TextReader? m_PreviousConsoleInput;

    [SetUp]
    public void SetUp()
    {
        m_MockPrompt.Reset();
        m_MockPersister.Reset();
        m_PreviousConsoleInput = Console.In;
    }

    [TearDown]
    public void TearDown()
    {
        Console.SetIn(m_PreviousConsoleInput!);
    }

    [Test]
    public void CreateTokenFormatsInputAsExpected()
    {
        var token = AuthenticatorV1.CreateToken(k_ValidServiceKeyId, k_ValidServiceSecretKey);

        Assert.AreEqual(k_AccessToken, token);
    }

    [Test]
    public async Task LoginAsyncWithServiceAccountSelectionPromptsAndPersistsExpectedToken()
    {
        var input = new LoginInput();
        m_MockPrompt.Setup(p => p.InteractiveEnabled).Returns(true);
        StubLoginMethodSelection(AuthenticatorV1.LoginMethodServiceAccount);
        m_MockPrompt.Setup(p => p.PromptAsync(AuthenticatorV1.KeyIdPrompt, CancellationToken.None))
            .ReturnsAsync(k_ValidServiceKeyId);
        m_MockPrompt.Setup(p => p.PromptAsync(AuthenticatorV1.SecretKeyPrompt, CancellationToken.None))
            .ReturnsAsync(k_ValidServiceSecretKey);

        var authenticatorV1 = new AuthenticatorV1(m_MockPersister.Object, m_MockPrompt.Object, new NullHubAuthProvider());

        await authenticatorV1.LoginAsync(input, CancellationToken.None);

        m_MockPersister.Verify(p => p.SaveAsync(k_AccessToken, CancellationToken.None), Times.Once);
    }

    [Test]
    public void LoginAsyncWithoutArgumentAndRedirectedStandardInputThrows()
    {
        var input = new LoginInput();
        m_MockPrompt.Setup(p => p.InteractiveEnabled)
            .Returns(false);

        var authenticatorV1 = new AuthenticatorV1(m_MockPersister.Object, m_MockPrompt.Object, new NullHubAuthProvider());

        Assert.ThrowsAsync<InvalidLoginInputException>(() => authenticatorV1.LoginAsync(input, CancellationToken.None));
    }

    [Test]
    public async Task LoginAsyncWithAllKeyOptionsAndRedirectedInputPersistsExpectedToken()
    {
        var input = new LoginInput
        {
            ServiceKeyId = k_ValidServiceKeyId,
            HasSecretKeyOption = true,
        };
        SetStandardInput(k_ValidServiceSecretKey);
        var authenticatorV1 = new AuthenticatorV1(m_MockPersister.Object, m_MockPrompt.Object, new NullHubAuthProvider());

        await authenticatorV1.LoginAsync(input, CancellationToken.None);

        m_MockPersister.Verify(p => p.SaveAsync(k_AccessToken, CancellationToken.None), Times.Once);
        m_MockPrompt.Verify(
            p => p.PromptAsync(It.IsAny<IPrompt<It.IsAnyType>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task PromptForServiceAccountKeysAsyncCallsExpectedPrompts()
    {
        m_MockPrompt.Setup(p => p.PromptAsync(AuthenticatorV1.KeyIdPrompt, CancellationToken.None))
            .ReturnsAsync(k_ValidServiceKeyId);
        m_MockPrompt.Setup(p => p.PromptAsync(AuthenticatorV1.SecretKeyPrompt, CancellationToken.None))
            .ReturnsAsync(k_ValidServiceSecretKey);

        var (keyId, secretKey) = await AuthenticatorV1.PromptForServiceAccountKeysAsync(m_MockPrompt.Object, CancellationToken.None);

        Assert.AreEqual(k_ValidServiceKeyId, keyId);
        Assert.AreEqual(k_ValidServiceSecretKey, secretKey);
    }

    [Test]
    public async Task ParseServiceAccountOptionsAsyncWithAllKeyOptionsAndRedirectedInputReturnsExpectedKeys()
    {
        var input = new LoginInput
        {
            ServiceKeyId = k_ValidServiceKeyId,
            HasSecretKeyOption = true,
        };
        SetStandardInput(k_ValidServiceSecretKey);

        var (keyId, secretKey) = await AuthenticatorV1.ParseServiceAccountOptionsAsync(input);

        Assert.AreEqual(k_ValidServiceKeyId, keyId);
        Assert.AreEqual(k_ValidServiceSecretKey, secretKey);
    }

    [TestCase(k_ValidServiceKeyId, false)]
    [TestCase(null, true)]
    public void ParseServiceAccountOptionsAsyncWithOnlyOneKeyOptionAndRedirectedInputThrows(
        string? keyId, bool hasSecretKey)
    {
        var input = new LoginInput
        {
            ServiceKeyId = keyId,
            HasSecretKeyOption = hasSecretKey,
        };
        SetStandardInput(k_ValidServiceSecretKey);

        Assert.ThrowsAsync<InvalidLoginInputException>(() => AuthenticatorV1.ParseServiceAccountOptionsAsync(input));
    }

    [TestCase("", k_ValidServiceSecretKey)]
    [TestCase(" ", k_ValidServiceSecretKey)]
    [TestCase(k_ValidServiceKeyId, "")]
    [TestCase(k_ValidServiceKeyId, " ")]
    public void ParseServiceAccountOptionsAsyncWithInvalidInputValuesThrows(string keyId, string secretKey)
    {
        // Note: testing non-redirected console input can't be done because it is always redirected in test suites.
        var input = new LoginInput
        {
            ServiceKeyId = keyId,
            HasSecretKeyOption = true,
        };
        SetStandardInput(secretKey);

        Assert.ThrowsAsync<InvalidLoginInputException>(() => AuthenticatorV1.ParseServiceAccountOptionsAsync(input));
    }

    [Test]
    public async Task LogoutAsyncDeletesPersistedToken()
    {
        Mock<ISystemEnvironmentProvider> mockEnvironmentProvider = new();
        var authenticator = CreateAuthenticator(out var persister);
        persister.PersistedToken = k_AccessToken;

        await authenticator.LogoutAsync(mockEnvironmentProvider.Object);

        Assert.IsNull(persister.PersistedToken);
    }

    [Test]
    public void GetTokenFromEnvironmentVariablesSucceed()
    {
        Mock<ISystemEnvironmentProvider> mockEnvironmentProvider = new();
        var sink = "";
        const string keyId = "key-id";
        const string secretKey = "secret-key";
        var expectedToken = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{keyId}:{secretKey}"));

        mockEnvironmentProvider.Setup(s => s.GetSystemEnvironmentVariable(AuthenticatorV1.ServiceKeyId, out sink))
            .Returns(keyId);
        mockEnvironmentProvider.Setup(s => s.GetSystemEnvironmentVariable(AuthenticatorV1.ServiceSecretKey, out sink))
            .Returns(secretKey);
        var actualToken = AuthenticatorV1.GetTokenFromEnvironmentVariables(mockEnvironmentProvider.Object, out var actualWarning);
        Assert.AreEqual(expectedToken, actualToken);
        // The warning out-param announces "env-var auth is in effect" whenever
        // any of the supported env vars yields a token. LogoutAsync reads the
        // same constant directly to surface the warning to the user.
        Assert.AreEqual(AuthenticatorV1.EnvironmentVariablesAndConfigSetWarning, actualWarning);
    }

    [Test]
    public void GetTokenFromEnvironmentVariablesReturnsBearerPrefixedJwtWhenAuthTokenSet()
    {
        Mock<ISystemEnvironmentProvider> mockEnvironmentProvider = new();
        var sink = "";
        const string jwt = "eyJhbGciOiJSUzI1NiJ9.payload.sig";

        mockEnvironmentProvider.Setup(s => s.GetSystemEnvironmentVariable(AuthenticatorV1.AuthToken, out sink))
            .Returns(jwt);

        var token = AuthenticatorV1.GetTokenFromEnvironmentVariables(mockEnvironmentProvider.Object, out _);

        Assert.AreEqual(AccessTokenHelper.BearerTokenSchemePrefix + jwt, token);
    }

    [Test]
    public void GetTokenFromEnvironmentVariablesPrefersAuthTokenOverServiceAccountKeys()
    {
        Mock<ISystemEnvironmentProvider> mockEnvironmentProvider = new();
        var sink = "";
        const string jwt = "eyJhbGciOiJSUzI1NiJ9.payload.sig";

        mockEnvironmentProvider.Setup(s => s.GetSystemEnvironmentVariable(AuthenticatorV1.AuthToken, out sink))
            .Returns(jwt);
        mockEnvironmentProvider.Setup(s => s.GetSystemEnvironmentVariable(AuthenticatorV1.ServiceKeyId, out sink))
            .Returns("ignored-key");
        mockEnvironmentProvider.Setup(s => s.GetSystemEnvironmentVariable(AuthenticatorV1.ServiceSecretKey, out sink))
            .Returns("ignored-secret");

        var token = AuthenticatorV1.GetTokenFromEnvironmentVariables(mockEnvironmentProvider.Object, out _);

        Assert.AreEqual(AccessTokenHelper.BearerTokenSchemePrefix + jwt, token);
    }

    [Test]
    public void GetTokenFromEnvironmentVariablesFallsBackToServiceAccountWhenAuthTokenBlank()
    {
        Mock<ISystemEnvironmentProvider> mockEnvironmentProvider = new();
        var sink = "";
        const string keyId = "key-id";
        const string secretKey = "secret-key";
        var expected = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{keyId}:{secretKey}"));

        mockEnvironmentProvider.Setup(s => s.GetSystemEnvironmentVariable(AuthenticatorV1.AuthToken, out sink))
            .Returns("   ");
        mockEnvironmentProvider.Setup(s => s.GetSystemEnvironmentVariable(AuthenticatorV1.ServiceKeyId, out sink))
            .Returns(keyId);
        mockEnvironmentProvider.Setup(s => s.GetSystemEnvironmentVariable(AuthenticatorV1.ServiceSecretKey, out sink))
            .Returns(secretKey);

        var token = AuthenticatorV1.GetTokenFromEnvironmentVariables(mockEnvironmentProvider.Object, out _);

        Assert.AreEqual(expected, token);
    }

    [Test]
    public async Task LogoutAsync()
    {
        Mock<ISystemEnvironmentProvider> mockEnvironmentProvider = new();
        const string keyId = "key-id";
        const string secretKey = "secret-key";

        var expectedError = "";
        mockEnvironmentProvider.Setup(s => s.GetSystemEnvironmentVariable(AuthenticatorV1.ServiceKeyId, out expectedError))
            .Returns(keyId);
        mockEnvironmentProvider.Setup(s => s.GetSystemEnvironmentVariable(AuthenticatorV1.ServiceSecretKey, out expectedError))
            .Returns(secretKey);

        var authenticator = CreateAuthenticator(out var persister);
        persister.PersistedToken = k_AccessToken;

        var response = await authenticator.LogoutAsync(mockEnvironmentProvider.Object);

        Assert.AreEqual("Login key cleared from local configuration.", response.Information);
        Assert.AreEqual(AuthenticatorV1.EnvironmentVariablesAndConfigSetWarning, response.Warning);
    }

    [Test]
    public async Task GetTokenAsyncLoadsPersistedToken()
    {
        var authenticator = CreateAuthenticator(out var persister);
        persister.PersistedToken = k_AccessToken;

        var token = await authenticator.GetTokenAsync();

        Assert.AreEqual(k_AccessToken, token);
    }

    AuthenticatorV1 CreateAuthenticator(out MemoryTokenPersister persister)
    {
        persister = new MemoryTokenPersister();
        return new AuthenticatorV1(persister, m_MockPrompt.Object, new NullHubAuthProvider());
    }

    void StubLoginMethodSelection(string method)
    {
        m_MockPrompt.Setup(p => p.SelectionPromptAsync(
                It.IsAny<string>(),
                It.IsAny<ICollection<string>>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<int>()))
            .ReturnsAsync(method);
    }

    static void SetStandardInput(string input)
    {
        var customIn = new StringReader(input);
        Console.SetIn(customIn);
    }

#if FEATURE_HUB_AUTH
    [Test]
    public async Task LoginAsyncUsesHubAuthWhenUserSelectsUnityHub()
    {
        var input = new LoginInput();
        m_MockPrompt.Setup(p => p.InteractiveEnabled).Returns(true);
        StubLoginMethodSelection(AuthenticatorV1.LoginMethodUnityHub);

        var mockHubProvider = new Mock<IHubAuthProvider>();
        mockHubProvider.Setup(h => h.TryLoginAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LoginResult("Test User"));

        var persister = new MemoryTokenPersister();
        var authenticator = new AuthenticatorV1(persister, m_MockPrompt.Object, mockHubProvider.Object);

        var result = await authenticator.LoginAsync(input, CancellationToken.None);

        Assert.AreEqual(AuthenticatorV1.HubAuthMarker, persister.PersistedToken);
        Assert.AreEqual("Test User", result.DisplayName);
    }


    [Test]
    public void LoginAsyncThrowsWhenUserSelectsUnityHubButHubUnavailable()
    {
        var input = new LoginInput();
        m_MockPrompt.Setup(p => p.InteractiveEnabled).Returns(true);
        StubLoginMethodSelection(AuthenticatorV1.LoginMethodUnityHub);

        var mockHubProvider = new Mock<IHubAuthProvider>();
        mockHubProvider.Setup(h => h.TryLoginAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((LoginResult?)null);

        var persister = new MemoryTokenPersister();
        var authenticator = new AuthenticatorV1(persister, m_MockPrompt.Object, mockHubProvider.Object);

        Assert.ThrowsAsync<HubIpcUnavailableException>(
            () => authenticator.LoginAsync(input, CancellationToken.None));
    }

    [Test]
    public async Task LoginAsyncNonInteractiveFallsBackToHubThenThrows()
    {
        var input = new LoginInput();
        m_MockPrompt.Setup(p => p.InteractiveEnabled).Returns(false);

        var mockHubProvider = new Mock<IHubAuthProvider>();
        mockHubProvider.Setup(h => h.TryLoginAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LoginResult("Test User"));

        var persister = new MemoryTokenPersister();
        var authenticator = new AuthenticatorV1(persister, m_MockPrompt.Object, mockHubProvider.Object);

        var result = await authenticator.LoginAsync(input, CancellationToken.None);

        Assert.AreEqual(AuthenticatorV1.HubAuthMarker, persister.PersistedToken);
        Assert.AreEqual("Test User", result.DisplayName);
    }

    [Test]
    public async Task LoginAsyncDoesNotTryHubWhenServiceKeyArgsPassed()
    {
        var input = new LoginInput
        {
            ServiceKeyId = k_ValidServiceKeyId,
            HasSecretKeyOption = true,
        };
        SetStandardInput(k_ValidServiceSecretKey);

        var mockHubProvider = new Mock<IHubAuthProvider>();
        var persister = new MemoryTokenPersister();
        var authenticator = new AuthenticatorV1(persister, m_MockPrompt.Object, mockHubProvider.Object);

        await authenticator.LoginAsync(input, CancellationToken.None);

        mockHubProvider.Verify(h => h.TryLoginAsync(It.IsAny<CancellationToken>()), Times.Never);
        Assert.AreEqual(k_AccessToken, persister.PersistedToken);
    }
#endif
}
