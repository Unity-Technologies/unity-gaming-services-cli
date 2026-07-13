using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Unity.Services.Cli.Common.Networking;
using Unity.Services.Cli.Common.Models;
using Unity.Services.Cli.IntegrationTest.Common;
using Unity.Services.Cli.MockServer;
using Unity.Services.Cli.MockServer.Common;

namespace Unity.Services.Cli.IntegrationTest;

/// <summary>
/// A test fixture to facilitate integration testing
/// </summary>
[TestFixture, Timeout(5 * 60 * 1000)]
public abstract class UgsCliFixture
{
#if DISABLE_CLI_REBUILD
    static Task? s_BuildCliTask = Task.CompletedTask;
#else
    static Task? s_BuildCliTask;
#endif

    static readonly SemaphoreSlim k_BuildLock = new(1, 1);
    static readonly ActivitySource k_TestActivity = new(nameof(UgsCliFixture));

    /// <summary>
    /// Returns the path to the configuration file used by the config module
    /// </summary>
    protected string ConfigurationFile => m_IntegrationConfig.ConfigurationFile;
    /// <summary>
    /// Returns the path to the configuration file used by the auth module
    /// </summary>
    protected string CredentialsFile => m_IntegrationConfig.CredentialsFile;
    /// <summary>
    /// Api server used for mocking service requests
    /// </summary>
    protected MockApi MockApi { get; private set; } = null!;

    readonly IntegrationConfig m_IntegrationConfig = new();
    Activity? m_CurrentTest;

    static UgsCliFixture()
    {
        ActivitySource.AddActivityListener(new()
        {
            ShouldListenTo = _ => true,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            ActivityStopped = activity =>
            {
                TestContext.Progress.WriteLine(
                    $"[Timing] {activity.OperationName} took {activity.Duration.TotalMilliseconds}ms");
            }
        });
    }

    [OneTimeSetUp]
    public async Task OneTimeSetUpBase()
    {
        MockApi = new MockApi();
        await BuildCliIfNeeded();
    }

    [OneTimeTearDown]
    public void OneTimeTearDownBase()
    {
        MockApi.Dispose();
        m_IntegrationConfig.Dispose();
    }

    [SetUp]
    public void TestOutputTrackingSetup()
    {
        m_CurrentTest = k_TestActivity.StartActivity(TestContext.CurrentContext.Test.Name);
    }

    [TearDown]
    public void TestOutputTrackingTeardown()
    {
        m_CurrentTest?.Dispose();
    }

    static async Task BuildCliIfNeeded()
    {
        using var build = k_TestActivity.StartActivity();

        await k_BuildLock.WaitAsync();
        try
        {
            if (s_BuildCliTask == null)
            {
                s_BuildCliTask = UgsCliBuilder.Build();
            }
        }
        finally
        {
            k_BuildLock.Release();
        }

        await s_BuildCliTask;
    }

    protected void SetConfigValue(string key, string value)
    {
        m_IntegrationConfig.SetConfigValue(key, value);
    }

    protected void DeleteLocalConfig()
    {
        if (File.Exists(ConfigurationFile))
        {
            File.Delete(ConfigurationFile);
        }
    }

    protected void DeleteLocalCredentials()
    {
        if (File.Exists(CredentialsFile))
        {
            File.Delete(CredentialsFile);
        }
    }

    protected void SetupProjectAndEnvironment()
    {
        SetConfigValue("project-id", CommonKeys.ValidProjectId);
        SetConfigValue("environment-name", CommonKeys.ValidEnvironmentName);
    }

    protected UgsCliTestCase NewUgsCliTestCase()
    {
        return new UgsCliTestCase()
            .WithEnvironmentVariables(new Dictionary<string, string?>
            {
                [Keys.EnvironmentKeys.ConfigDir] = m_IntegrationConfig.ConfigDir,
                [Keys.EnvironmentKeys.MockServerUrl] = MockApi.Url
            });
    }

    protected UgsCliTestCase GetLoggedInCli()
    {
        return NewUgsCliTestCase()
            .Command($"login --service-key-id {CommonKeys.ValidServiceAccKeyId} --secret-key-stdin")
            .StandardInputWriteLine(CommonKeys.ValidServiceAccSecretKey);
    }

    protected UgsCliTestCase GetFullySetCli()
    {
        SetupProjectAndEnvironment();
        return GetLoggedInCli();
    }
}
