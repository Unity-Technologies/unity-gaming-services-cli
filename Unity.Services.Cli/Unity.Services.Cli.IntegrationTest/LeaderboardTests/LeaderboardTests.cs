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
    static readonly string k_LeaderboardFileName = "foo";
    static readonly string k_MissingFieldLeaderboardFileName = "missing";
    static readonly string k_brokenFile = "broken";

    static readonly string k_defaultFileName = "ugs.lbzip";
    static readonly string k_alternateFileName = "other.lbzip";

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
        await File.WriteAllTextAsync(Path.Join(k_TestDirectory, k_LeaderboardFileName), JsonConvert.SerializeObject(LeaderboardApiMock.Leaderboard1));
        await File.WriteAllTextAsync(Path.Join(k_TestDirectory, k_MissingFieldLeaderboardFileName), "{ \"id\": \"lb1\", \"name\": \"leaderboard 1\" }");
        await File.WriteAllTextAsync(Path.Join(k_TestDirectory, k_brokenFile), "{");

        m_MockApi.Server?.ResetMappings();
        await m_MockApi.MockServiceAsync(new IdentityV1Mock());
        await m_MockApi.MockServiceAsync(new LeaderboardApiMock());
    }

    [Test]
    public async Task LeaderboardListSucceed()
    {
        var expectedResult = @"Fetching leaderboard list...
[
  {
    ""Id"": ""lb1"",
    ""Name"": ""leaderboard 1""
  },
  {
    ""Id"": ""lb2"",
    ""Name"": ""leaderboard 2""
  }
]";
        await AssertSuccess("leaderboards list", expectedResult: expectedResult);
    }

    [Test]
    public async Task LeaderboardListSucceedJson()
    {
        var expectedResult = @"{
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
}";
        await AssertSuccess("leaderboards list -j", expectedResult: expectedResult);
    }

    [Test]
    public async Task LeaderboardListSucceedWithOption()
    {
        DeleteLocalConfig();
        DeleteLocalCredentials();

        var expectedResult = @"Fetching leaderboard list...
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
        await AssertSuccess($"leaderboards list -p {CommonKeys.ValidProjectId} -e {CommonKeys.ValidEnvironmentName}", expectedResult: expectedResult);
    }

    [Test]
    public async Task LeaderboardGetSucceed()
    {
        var expectedResult =
            @"Fetching leaderboard info...
sortOrder: Asc
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
        await AssertSuccess("leaderboards get lb1", expectedResult: expectedResult);
    }

    [Test]
    public async Task LeaderboardGetSucceedJSon()
    {
        var expectedResult = @"{
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
}";
        await AssertSuccess("leaderboards get lb1 -j", expectedResult: expectedResult);
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

    [Test]
    public async Task LeaderboardImportSucceed()
    {
        ZipArchiver m_zipArchiver = new ZipArchiver();
        await m_zipArchiver.ZipAsync(Path.Join(k_TestDirectory, k_defaultFileName), "test", new[]
            {  LeaderboardApiMock.Leaderboard1, LeaderboardApiMock.Leaderboard2, LeaderboardApiMock.Leaderboard3,
                LeaderboardApiMock.Leaderboard4, LeaderboardApiMock.Leaderboard5, LeaderboardApiMock.Leaderboard6,
                LeaderboardApiMock.Leaderboard7, LeaderboardApiMock.Leaderboard8, LeaderboardApiMock.Leaderboard9,
                LeaderboardApiMock.Leaderboard10, LeaderboardApiMock.Leaderboard11, LeaderboardApiMock.Leaderboard12 });

        var expectedMessage = "Importing configs...";
        await AssertSuccess($"leaderboards import {k_TestDirectory}", expectedResult: expectedMessage);
    }

    [Test]
    public async Task LeaderboardImportWithNameSucceed()
    {
        ZipArchiver m_zipArchiver = new ZipArchiver();
        await m_zipArchiver.ZipAsync(Path.Join(k_TestDirectory, k_alternateFileName), "test", new[] { LeaderboardApiMock.Leaderboard1, LeaderboardApiMock.Leaderboard2, LeaderboardApiMock.Leaderboard3, LeaderboardApiMock.Leaderboard4, LeaderboardApiMock.Leaderboard5, LeaderboardApiMock.Leaderboard6, LeaderboardApiMock.Leaderboard7, LeaderboardApiMock.Leaderboard8, LeaderboardApiMock.Leaderboard9, LeaderboardApiMock.Leaderboard10, LeaderboardApiMock.Leaderboard11, LeaderboardApiMock.Leaderboard12 });

        var expectedMessage = "Importing configs...";
        await AssertSuccess($"leaderboards import {k_TestDirectory} {k_alternateFileName}", expectedResult: expectedMessage);
    }

    [Test]
    public async Task LeaderboardExportSucceed()
    {
        var expectedMessage = "Exporting your environment...";
        await AssertSuccess($"leaderboards export {k_TestDirectory}", expectedResult: expectedMessage);
    }

    [Test]
    public async Task LeaderboardExportWithNameSucceed()
    {
        var expectedMessage = "Exporting your environment...";
        await AssertSuccess($"leaderboards export {k_TestDirectory} {k_alternateFileName}", expectedResult: expectedMessage);
    }

    [Test]
    public async Task LeaderboardExportWithSameNameSucceed()
    {
        var expectedMessage = "Exporting your environment...";
        await AssertSuccess($"leaderboards export {k_TestDirectory} {k_alternateFileName}", expectedResult: expectedMessage);
        var errorMessage = "The filename to export to already exists. Please create a new file";
        await AssertException($"leaderboards export {k_TestDirectory} {k_alternateFileName}", errorMessage);
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

    static async Task AssertSuccess(string command, string? expectedMessage = null, string? expectedResult = null)
    {
        var test = GetLoggedInCli()
            .Command(command);
        if (expectedMessage != null)
        {
            test = test.AssertStandardErrorContains(expectedMessage.ReplaceLineEndings());
        }

        if (expectedResult != null)
        {
            test = test.AssertStandardOutputContains(expectedResult.ReplaceLineEndings());
        }
        await test.ExecuteAsync();
    }

    static async Task AssertException(string command, string expectedMessage)
    {
        await GetLoggedInCli()
            .Command(command)
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardErrorContains(expectedMessage)
            .ExecuteAsync();
    }
}
