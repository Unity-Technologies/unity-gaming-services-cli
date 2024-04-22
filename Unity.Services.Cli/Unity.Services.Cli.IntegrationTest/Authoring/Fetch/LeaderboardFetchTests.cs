using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Unity.Services.Cli.Authoring.Model;
using Unity.Services.Cli.Leaderboards.Deploy;
using Unity.Services.Cli.MockServer.Common;
using Unity.Services.Cli.MockServer.ServiceMocks;
using Unity.Services.DeploymentApi.Editor;
using Unity.Services.Leaderboards.Authoring.Core.Model;
using Unity.Services.Leaderboards.Authoring.Core.Validations;
using Statuses = Unity.Services.Cli.Authoring.Model.Statuses;

namespace Unity.Services.Cli.IntegrationTest.Authoring.Fetch;

public class LeaderboardFetchTests : UgsCliFixture
{
    static readonly string k_TestDirectory = Path.Combine(UgsCliBuilder.RootDirectory, ".tmp", "FilesDir");
    LeaderboardConfig[]? m_LocalLeaderboards;
    LeaderboardConfig[]? m_RemoteLeaderboards;

    [SetUp]
    public async Task SetUp()
    {
        DeleteLocalConfig();
        DeleteLocalCredentials();

        if (Directory.Exists(k_TestDirectory))
        {
            Directory.Delete(k_TestDirectory, true);
        }

        MockApi.Server?.ResetMappings();
        await MockApi.MockServiceAsync(new LeaderboardApiMock());
        await MockApi.MockServiceAsync(new IdentityV1Mock());
        Directory.CreateDirectory(k_TestDirectory);
        m_LocalLeaderboards = new LeaderboardConfig[]
        {
            new ("lb1", "leaderboard 1") { Path = Path.Combine(k_TestDirectory, "lb1.lb") }
        };

        m_RemoteLeaderboards = new LeaderboardConfig[]
        {
            new ("lb1", "leaderboard 1") { Path = Path.Combine(k_TestDirectory, "lb1.lb") },
            new ("lb2", "leaderboard 2") { Path = Path.Combine(k_TestDirectory, "lb2.lb") }
        };
    }

    [TearDown]
    public void TearDown()
    {
        DeleteLocalConfig();
        DeleteLocalCredentials();

        if (Directory.Exists(k_TestDirectory))
        {
            Directory.Delete(k_TestDirectory, true);
        }
    }

    static async Task CreateDeployTestFilesAsync(IReadOnlyList<LeaderboardConfig> testCases)
    {
        var serializer = new LeaderboardsSerializer();
        foreach (var testCase in testCases)
        {
            var test = serializer.Serialize(testCase);
            await File.WriteAllTextAsync(testCase.Path, test);
        }
    }

    [Test]
    public async Task FetchToValidConfigFromDirectorySucceeds()
    {
        var localLeaderboards = m_LocalLeaderboards!;
        await CreateDeployTestFilesAsync(localLeaderboards);
        var expectedResult = new FetchResult(
                updated: new IDeploymentItem[] { localLeaderboards[0] },
                deleted: Array.Empty<IDeploymentItem>(),
                created: Array.Empty<IDeploymentItem>(),
                authored: new IDeploymentItem[] { localLeaderboards[0] },
                failed: Array.Empty<IDeploymentItem>()
            );
        await GetFullySetCli()
            .Command($"fetch {k_TestDirectory} -s leaderboards")
            .AssertStandardOutputContains(expectedResult.ToString())
            .AssertNoErrors()
            .ExecuteAsync();
    }

    [Test]
    public async Task FetchToValidConfigFromDirectoryReconcileSucceeds()
    {
        var localLeaderboards = m_LocalLeaderboards!;
        await CreateDeployTestFilesAsync(localLeaderboards);
        var expectedResult = new FetchResult(
            updated: new IDeploymentItem[] { localLeaderboards[0] },
            deleted: Array.Empty<IDeploymentItem>(),
            created: new IDeploymentItem[] { m_RemoteLeaderboards![1] },
            authored: new IDeploymentItem[] { localLeaderboards[0], m_RemoteLeaderboards![1] },
            failed: Array.Empty<IDeploymentItem>()
        );
        await GetFullySetCli()
            .Command($"fetch {k_TestDirectory} --reconcile -s leaderboards")
            .AssertStandardOutputContains(expectedResult.ToString())
            .AssertNoErrors()
            .ExecuteAsync();
    }

    [Test]
    public async Task FetchToValidConfigFromDirectoryDryRunSucceeds()
    {
        var localLeaderboards = m_LocalLeaderboards!;
        await CreateDeployTestFilesAsync(localLeaderboards);
        var expectedResult = new FetchResult(
            updated: new IDeploymentItem[] { localLeaderboards[0] },
            deleted: Array.Empty<IDeploymentItem>(),
            created: Array.Empty<IDeploymentItem>(),
            authored: Array.Empty<IDeploymentItem>(),
            failed: Array.Empty<IDeploymentItem>(),
            dryRun: true
        );
        await GetFullySetCli()
            .Command($"fetch {k_TestDirectory} --dry-run -s leaderboards")
            .AssertStandardOutputContains(expectedResult.ToString())
            .AssertNoErrors()
            .ExecuteAsync();
    }

    [Test]
    public async Task FetchToValidConfigFromDirectoryDryRunWithReconcileSucceeds()
    {
        var localLeaderboards = m_LocalLeaderboards!;
        await CreateDeployTestFilesAsync(localLeaderboards);
        var expectedResult = new FetchResult(
            updated: new IDeploymentItem[] { localLeaderboards[0] },
            deleted: Array.Empty<IDeploymentItem>(),
            created: new IDeploymentItem[] { m_RemoteLeaderboards![1] },
            authored: Array.Empty<IDeploymentItem>(),
            failed: Array.Empty<IDeploymentItem>(),
            dryRun: true
        );
        await GetFullySetCli()
            .Command($"fetch {k_TestDirectory} --reconcile --dry-run -s leaderboards")
            .AssertStandardOutputContains(expectedResult.ToString())
            .AssertNoErrors()
            .ExecuteAsync();
    }

    [Test]
    public async Task FetchToValidConfigFromDuplicateIdFails()
    {
        var localLeaderboards = m_LocalLeaderboards!.Append(
            new LeaderboardConfig("lb1", "eh")
            {
                Path = Path.Combine(k_TestDirectory, "lb11.lb")
            }).ToList();
        await CreateDeployTestFilesAsync(localLeaderboards);

        foreach (var lb in localLeaderboards)
        {
            var failedMessage1 =
                DuplicateResourceValidation.GetDuplicateResourceErrorMessages(lb, localLeaderboards);
            var failedStatus = Statuses.GetFailedToFetch(failedMessage1.Item2);
            lb.Status = failedStatus;
        }

        await CreateDeployTestFilesAsync(localLeaderboards);
        var expectedResult = new FetchResult(
            updated: Array.Empty<IDeploymentItem>(),
            deleted: Array.Empty<IDeploymentItem>(),
            created: Array.Empty<IDeploymentItem>(),
            authored: Array.Empty<IDeploymentItem>(),
            failed: new IDeploymentItem[] { localLeaderboards[0], localLeaderboards[1] }
        );
        await GetFullySetCli()
            .Command($"fetch {k_TestDirectory} -s leaderboards")
            .AssertStandardOutputContains(expectedResult.ToString())
            .ExecuteAsync();
    }
}
