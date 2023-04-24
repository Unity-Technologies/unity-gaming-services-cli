using System;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NUnit.Framework;
using Unity.Services.Cli.MockServer.Common;

namespace Unity.Services.Cli.IntegrationTest.AuthTests;

public class AuthTests : UgsCliFixture
{
    const string k_InteractiveLoginTestCase =
        "https://qatestrail.hq.unity3d.com/index.php?/cases/view/1178164";

    [SetUp]
    public void Setup()
    {
        DeleteLocalConfig();
        DeleteLocalCredentials();
    }

    [TearDown]
    public void TearDown()
    {
        LogoutFromEnvironment();
    }

    [Test]
    public async Task AuthLoginWithCredentialsSavesToken()
    {
        await GetLoggedInCli()
            .AssertNoErrors()
            .WaitForExit(() => AssertToken(CommonKeys.ValidAccessToken))
            .ExecuteAsync();
    }

    [Test]
    [Ignore($"Please test it manually in {k_InteractiveLoginTestCase}. Look for solution automate this in future")]
    public async Task AuthLoginWithoutOption()
    {
        await new UgsCliTestCase()
            .Command("login")
            .StandardInputWriteLine($"{CommonKeys.ValidServiceAccKeyId}")
            .StandardInputWriteLine($"{CommonKeys.ValidServiceAccSecretKey}")
            .AssertStandardOutput(output =>
            {
                StringAssert.Contains($"Enter your key-id: {CommonKeys.ValidServiceAccKeyId}", output);
                StringAssert.Contains($"Enter your secret-key: {new string('*', CommonKeys.ValidServiceAccSecretKey.Length)}", output);
            })
            .ExecuteAsync();
    }

    [Test]
    public async Task AuthLogoutRemovesToken()
    {
        await GetLoggedInCli()
            .Command("logout")
            .AssertNoErrors()
            .WaitForExit(AssertTokenNotSaved)
            .AssertStandardOutputContains("Service Account key cleared from local configuration.")
            .ExecuteAsync();
    }

    [Test]
    public async Task AuthLogoutWithCredentialEnvironmentVariablesSetPrintsLoggedOut()
    {
        LoginWithEnvironment();
        await GetLoggedInCli()
            .Command("logout")
            .AssertNoErrors()
            .WaitForExit(AssertTokenNotSaved)
            .AssertStandardOutput(output =>
                {
                    StringAssert.Contains("Service Account key cleared from local configuration.", output);
                    StringAssert.Contains("Because UGS_CLI_SERVICE_KEY_ID and" +
                                          " UGS_CLI_SERVICE_SECRET_KEY are set, you will still be able to make" +
                                          " authenticated service calls. Clear login-related system environment" +
                                          " variables to fully logout.", output);
                })
            .ExecuteAsync();
    }

    [Test]
    public async Task AuthStatusReturnsLoggedOut()
    {
        await new UgsCliTestCase()
            .Command("status")
            .AssertNoErrors()
            .AssertStandardOutputContains("No Service Account key stored.")
            .ExecuteAsync();
    }

    [Test]
    public async Task AuthStatusReturnsLoggedIn()
    {
        await GetLoggedInCli()
            .Command("status")
            .AssertNoErrors()
            .AssertStandardOutputContains("Using Service Account key from local configuration.")
            .ExecuteAsync();
    }

    [Test]
    public async Task AuthStatusReturnsLoggedInWithLocalConfiguration()
    {
        LoginWithEnvironment();
        await GetLoggedInCli()
            .Command("status")
            .AssertNoErrors()
            .AssertStandardOutputContains("Using Service Account key from local configuration.")
            .ExecuteAsync();
    }

    [Test]
    public async Task AuthStatusReturnsLoggedInWithEnvironmentVariables()
    {
        LoginWithEnvironment();
        await new UgsCliTestCase()
            .Command("status")
            .AssertNoErrors()
            .AssertStandardOutputContains("Using Service Account key from system environment variables.")
            .ExecuteAsync();
    }

    void AssertToken(string token)
    {
        var content = File.ReadAllText(CredentialsFile!);
        var savedToken = JsonConvert.DeserializeObject<string>(content);
        Assert.AreEqual(token, savedToken);
    }

    void AssertTokenNotSaved()
    {
        Assert.False(File.Exists(CredentialsFile));
    }

    static void LoginWithEnvironment()
    {
        System.Environment.SetEnvironmentVariable("UGS_CLI_SERVICE_KEY_ID", CommonKeys.ValidServiceAccKeyId);
        System.Environment.SetEnvironmentVariable("UGS_CLI_SERVICE_SECRET_KEY", CommonKeys.ValidServiceAccSecretKey);
    }

    static void LogoutFromEnvironment()
    {
        System.Environment.SetEnvironmentVariable("UGS_CLI_SERVICE_KEY_ID", null);
        System.Environment.SetEnvironmentVariable("UGS_CLI_SERVICE_SECRET_KEY", null);
    }
}
