using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NUnit.Framework;
using Unity.Services.Cli.Common.Exceptions;
using Unity.Services.Cli.Authoring.Compression;
using Unity.Services.Cli.IntegrationTest.EnvTests;
using Unity.Services.Cli.MockServer;
using Unity.Services.Gateway.LeaderboardApiV1.Generated.Model;

namespace Unity.Services.Cli.IntegrationTest.LeaderboardTests;

public class LeaderboardTests : UgsCliFixture
{
    static readonly string k_TestDirectory = Path.Combine(UgsCliBuilder.RootDirectory, ".tmp/FilesDir");
    private static readonly string k_LeaderboardFileName = "foo";
    static readonly string k_LearedboardDefaultZipFileName = "ugs";
    private static readonly string k_LeaderboardZipFileName = "test_name";
    private static readonly string k_MissingFieldLeaderboardFileName = "missing";
    private static readonly string k_brokenFile = "broken";
    readonly LeaderboardApiMock m_LeaderboardApiMock = new(CommonKeys.ValidProjectId, CommonKeys.ValidEnvironmentId);
    readonly UpdatedLeaderboardConfig m_Leaderboard = new(
        "lb1",
        "leaderboard 1",
        SortOrder.Asc,
        UpdateType.Aggregate,
        10,
        new ResetConfig(new DateTime(2023, 1, 1), "@1d", true)
    );
    readonly UpdatedLeaderboardConfig m_Leaderboard2 = new(
        "lb2",
        "leaderboard 2",
        SortOrder.Asc,
        UpdateType.Aggregate,
        10,
        new ResetConfig(new DateTime(2023, 1, 1), "@1d", true)
    );

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        m_LeaderboardApiMock.MockApi.InitServer();
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        m_LeaderboardApiMock.MockApi.Server?.Dispose();
        if (Directory.Exists(k_TestDirectory))
        {
            Directory.Delete(k_TestDirectory, true);
        }
    }

    [SetUp]
    public async Task SetUp()
    {
        DeleteLocalConfig();
        DeleteLocalCredentials();
        SetupProjectAndEnvironment();

        if (Directory.Exists(k_TestDirectory))
        {
            Directory.Delete(k_TestDirectory, true);
        }
        Directory.CreateDirectory(k_TestDirectory);
        await File.WriteAllTextAsync(k_TestDirectory + "/" + k_LeaderboardFileName, JsonConvert.SerializeObject(m_Leaderboard));
        await File.WriteAllTextAsync(k_TestDirectory + "/" + k_MissingFieldLeaderboardFileName, "{ \"id\": \"lb1\", \"name\": \"leaderboard 1\" }");
        await File.WriteAllTextAsync(k_TestDirectory + "/" + k_brokenFile, "{");

        ZipArchiver<UpdatedLeaderboardConfig> m_zipArchiver = new ZipArchiver<UpdatedLeaderboardConfig>();
        m_zipArchiver.Zip(k_TestDirectory +"/" + k_LearedboardDefaultZipFileName, k_LearedboardDefaultZipFileName, "test", "lbzip", new[]{m_Leaderboard});
        m_zipArchiver.Zip(k_TestDirectory +"/" + k_LeaderboardZipFileName, k_LearedboardDefaultZipFileName, "test", "lbzip", new[]{m_Leaderboard});

        m_LeaderboardApiMock.MockApi.Server?.ResetMappings();
        var environmentModels = await IdentityV1MockServerModels.GetModels();
        environmentModels = environmentModels.Select(
            m => m.ConfigMappingPathWithKey(CommonKeys.ProjectIdKey, CommonKeys.ValidProjectId)
                .ConfigMappingPathWithKey(CommonKeys.EnvironmentIdKey, CommonKeys.ValidEnvironmentId));
        m_LeaderboardApiMock.MockApi.Server?.WithMapping(environmentModels.ToArray());
    }

    [Test]
    public async Task LeaderboardListSucceed()
    {
        m_LeaderboardApiMock.MockListLeaderboards(new List<UpdatedLeaderboardConfig>()
        {
            m_Leaderboard,
            m_Leaderboard2
        });

        var expectedMessage = $"\"leaderboard 1\": \"lb1\"{Environment.NewLine}\"leaderboard 2\": \"lb2\"";
        await AssertSuccess("leaderboards list", expectedMessage);
    }

    [Test]
    public async Task LeaderboardListSucceedWithOption()
    {
        DeleteLocalConfig();
        DeleteLocalCredentials();
        m_LeaderboardApiMock.MockListLeaderboards(new List<UpdatedLeaderboardConfig>()
        {
            m_Leaderboard,
            m_Leaderboard2
        });

        var expectedMessage = $"\"leaderboard 1\": \"lb1\"{Environment.NewLine}\"leaderboard 2\": \"lb2\"";
        await AssertSuccess($"leaderboards list -p {CommonKeys.ValidProjectId} -e {CommonKeys.ValidEnvironmentName}", expectedMessage);
    }

    [Test]
    public async Task LeaderboardListApiException()
    {
        m_LeaderboardApiMock.MockListLeaderboards(new List<UpdatedLeaderboardConfig>()
        {
            m_Leaderboard,
            m_Leaderboard2
        }, HttpStatusCode.NotFound);

        await AssertApiException("leaderboards list");
    }

    [Test]
    public async Task LeaderboardGetSucceed()
    {
        m_LeaderboardApiMock.MockGetLeaderboard(m_Leaderboard);

        var expectedMessage =
            @"sortOrder: Asc
updateType: Aggregate
id: lb1
name: leaderboard 1
bucketSize: 10.0
resetConfig:
  start: 2023-01-01T00:00:00.0000000
  schedule: '@1d'
  archive: true
tieringConfig:";
        await AssertSuccess("leaderboards get lb1", expectedMessage);
    }

    [Test]
    public async Task LeaderboardGetApiException()
    {
        m_LeaderboardApiMock.MockGetLeaderboard(m_Leaderboard, HttpStatusCode.NotFound);
        await AssertApiException("leaderboards get lb1");
    }

    [Test]
    public async Task LeaderboardDeleteSucceed()
    {
        m_LeaderboardApiMock.MockDeleteLeaderboard("lb1");

        var expectedMessage = "leaderboard deleted!";
        await AssertSuccess("leaderboards delete lb1", expectedMessage);
    }

    [Test]
    public async Task LeaderboardDeleteApiException()
    {
        m_LeaderboardApiMock.MockDeleteLeaderboard("lb1", HttpStatusCode.NotFound);
        await AssertApiException("leaderboards delete lb1");
    }

    [Test]
    public async Task LeaderboardResetSucceed()
    {
        m_LeaderboardApiMock.MockResetLeaderboard("lb1", "v10");

        var expectedMessage = "leaderboard reset! Version Id: v10";
        await AssertSuccess("leaderboards reset lb1", expectedMessage);
    }

    [Test]
    public async Task LeaderboardResetApiException()
    {
        m_LeaderboardApiMock.MockResetLeaderboard("lb1", "v10", HttpStatusCode.InternalServerError);
        await AssertApiException("leaderboards reset lb1");
    }

    [Test]
    public async Task LeaderboardCreateSucceed()
    {
        m_LeaderboardApiMock.MockCreateLeaderboard(m_Leaderboard);

        var expectedMessage = "leaderboard created!";
        await AssertSuccess($"leaderboards create {k_TestDirectory}/{k_LeaderboardFileName}", expectedMessage);
    }

    [Test]
    public async Task LeaderboardCreateApiException()
    {
        m_LeaderboardApiMock.MockCreateLeaderboard(m_Leaderboard, HttpStatusCode.InternalServerError);
        await AssertApiException($"leaderboards create {k_TestDirectory}/{k_LeaderboardFileName}");
    }

    [Test]
    public async Task LeaderboardUpdateSucceed()
    {
        m_LeaderboardApiMock.MockUpdateLeaderboard(m_Leaderboard);

        var expectedMessage = "leaderboard updated!";
        await AssertSuccess($"leaderboards update lb1 {k_TestDirectory}/{k_LeaderboardFileName}", expectedMessage);
    }

    [Test]
    public async Task LeaderboardUpdateApiException()
    {
        m_LeaderboardApiMock.MockUpdateLeaderboard(m_Leaderboard, HttpStatusCode.NotFound);
        await AssertApiException($"leaderboards update lb1 {k_TestDirectory}/{k_LeaderboardFileName}");
    }


    [TestCase("create")]
    [TestCase("update foo")]
    public async Task LeaderboardInvalidFilePath(string command)
    {
        var expectedMessage = "Invalid file path.";
        await AssertException($"leaderboards {command} /InvalidFilePath/foo.lb", expectedMessage);
    }

    [TestCase("create")]
    [TestCase("update foo")]
    public async Task LeaderboardWrongField(string command)
    {
        var expectedMessage = "Failed to deserialize object for Leaderboard request: Unexpected end when reading JSON.";
        await AssertException($"leaderboards {command} {k_TestDirectory}/{k_brokenFile}", expectedMessage);
    }

    [Test]
    public async Task LeaderboardCreateMissingRequiredField()
    {
        var expectedMessage = "Failed to deserialize object for Leaderboard request: Required property 'sortOrder' not found";
        await AssertException($"leaderboards create {k_TestDirectory}/{k_MissingFieldLeaderboardFileName}", expectedMessage);

    }

    private static async Task AssertSuccess(string command, string expectedMessage)
    {
        await GetLoggedInCli()
            .Command(command)
            .AssertNoErrors()
            .AssertStandardOutputContains(expectedMessage)
            .ExecuteAsync();
    }

    private static async Task AssertApiException(string command)
    {
        await GetLoggedInCli()
            .Command(command)
            .AssertExitCode(ExitCode.HandledError)
            .ExecuteAsync();
    }

    private static async Task AssertException(string command, string expectedMessage)
    {
        await GetLoggedInCli()
            .Command(command)
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardOutputContains(expectedMessage)
            .ExecuteAsync();
    }
}

