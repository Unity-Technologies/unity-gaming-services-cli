using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NUnit.Framework;
using Unity.Services.Cli.Common.Exceptions;
using Unity.Services.Cli.Authoring.Compression;
using Unity.Services.Cli.Common.Networking;
using Unity.Services.Cli.MockServer;
using Unity.Services.Cli.MockServer.Common;
using Unity.Services.Cli.MockServer.ServiceMocks;
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

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
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
        await File.WriteAllTextAsync(k_TestDirectory + "/" + k_LeaderboardFileName, JsonConvert.SerializeObject(LeaderboardApiMock.Leaderboard1));
        await File.WriteAllTextAsync(k_TestDirectory + "/" + k_MissingFieldLeaderboardFileName, "{ \"id\": \"lb1\", \"name\": \"leaderboard 1\" }");
        await File.WriteAllTextAsync(k_TestDirectory + "/" + k_brokenFile, "{");

        ZipArchiver<UpdatedLeaderboardConfig> m_zipArchiver = new ZipArchiver<UpdatedLeaderboardConfig>();
        m_zipArchiver.Zip(k_TestDirectory + "/" + k_LearedboardDefaultZipFileName, k_LearedboardDefaultZipFileName, "test", "lbzip", new[] { LeaderboardApiMock.Leaderboard1 });
        m_zipArchiver.Zip(k_TestDirectory + "/" + k_LeaderboardZipFileName, k_LearedboardDefaultZipFileName, "test", "lbzip", new[] { LeaderboardApiMock.Leaderboard1 });

        m_MockApi.Server?.ResetMappings();
        await m_MockApi.MockServiceAsync(new IdentityV1Mock());
        await m_MockApi.MockServiceAsync(new LeaderboardApiMock());
    }

    [Test]
    public async Task LeaderboardListSucceed()
    {
        var expectedMessage = @"Fetching leaderboard list...
[
  {
    ""Id"": ""lb1"",
    ""Name"": ""leaderboard 1""
  },
  {
    ""Id"": ""lb2"",
    ""Name"": ""leaderboard 2""
  }
]
";
        await AssertSuccess("leaderboards list", expectedMessage);
    }

    [Test]
    public async Task LeaderboardListSucceedJson()
    {
        var expectedMessage = @"{
  ""Result"": {
    ""Leaderboards"": [
      {
        ""Id"": ""lb1"",
        ""Name"": ""leaderboard 1""
      },
      {
        ""Id"": ""lb2"",
        ""Name"": ""leaderboard 2""
      }
    ]
  },
  ""Messages"": []
}
";
        await AssertSuccess("leaderboards list -j", expectedMessage);
    }

    [Test]
    public async Task LeaderboardListSucceedWithOption()
    {
        DeleteLocalConfig();
        DeleteLocalCredentials();

        var expectedMessage = @"Fetching leaderboard list...
[
  {
    ""Id"": ""lb1"",
    ""Name"": ""leaderboard 1""
  },
  {
    ""Id"": ""lb2"",
    ""Name"": ""leaderboard 2""
  }
]
";
        await AssertSuccess($"leaderboards list -p {CommonKeys.ValidProjectId} -e {CommonKeys.ValidEnvironmentName}", expectedMessage);
    }

    [Test]
    public async Task LeaderboardGetSucceed()
    {
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
tieringConfig:
  strategy: Percent
  tiers:
  - id: tier1
    cutoff: 2
updated: 0001-01-01T00:00:00.0000000
created: 0001-01-01T00:00:00.0000000
lastReset: 0001-01-01T00:00:00.0000000
versions: []";
        await AssertSuccess("leaderboards get lb1", expectedMessage);
    }

    [Test]
    public async Task LeaderboardGetSucceedJSon()
    {
        var expectedMessage = @"{
  ""Result"": {
    ""SortOrder"": ""asc"",
    ""UpdateType"": ""aggregate"",
    ""Id"": ""lb1"",
    ""Name"": ""leaderboard 1"",
    ""BucketSize"": 10.0,
    ""ResetConfig"": {
      ""start"": ""2023-01-01T00:00:00"",
      ""schedule"": ""@1d"",
      ""archive"": true
    },
    ""TieringConfig"": {
      ""strategy"": ""percent"",
      ""tiers"": [
        {
          ""id"": ""tier1"",
          ""cutoff"": 2.0
        }
      ]
    },
    ""Updated"": ""0001-01-01T00:00:00"",
    ""Created"": ""0001-01-01T00:00:00"",
    ""LastReset"": ""0001-01-01T00:00:00"",
    ""Versions"": []
  },
  ""Messages"": []
}
";
        await AssertSuccess("leaderboards get lb1 -j", expectedMessage);
    }

    [Test]
    public async Task LeaderboardDeleteSucceed()
    {
        var expectedMessage = "leaderboard deleted!";
        await AssertSuccess("leaderboards delete lb1", expectedMessage);
    }

    [Test]
    public async Task LeaderboardResetSucceed()
    {
        var expectedMessage = "leaderboard reset! Version Id: v10";
        await AssertSuccess("leaderboards reset lb1", expectedMessage);
    }

    [Test]
    public async Task LeaderboardCreateSucceed()
    {

        var expectedMessage = "leaderboard created!";
        await AssertSuccess($"leaderboards create {k_TestDirectory}/{k_LeaderboardFileName}", expectedMessage);
    }

    [Test]
    public async Task LeaderboardUpdateSucceed()
    {
        var expectedMessage = "leaderboard updated!";
        await AssertSuccess($"leaderboards update lb1 {k_TestDirectory}/{k_LeaderboardFileName}", expectedMessage);
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

