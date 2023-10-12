using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NUnit.Framework;
using Unity.Services.Cli.MockServer.ServiceMocks;
using Unity.Services.Cli.Triggers.Deploy;
using Unity.Services.DeploymentApi.Editor;
using Unity.Services.Triggers.Authoring.Core.Model;
using Statuses = Unity.Services.Cli.Authoring.Model.Statuses;
using Unity.Services.Triggers.Authoring.Core.Validations;

namespace Unity.Services.Cli.IntegrationTest.Authoring.Fetch;

[Ignore("Excluding fetch from the release")]
public class TriggersFetchTests : UgsCliFixture
{
    static readonly string k_TestDirectory =
        Path.Combine(UgsCliBuilder.RootDirectory, ".tmp", "FilesDir");
    TriggerConfig[] m_LocalTriggers = null!;
    TriggerConfig[] m_RemoteTriggers = null!;

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
        await MockApi.MockServiceAsync(new TriggersApiMock());
        await MockApi.MockServiceAsync(new IdentityV1Mock());
        Directory.CreateDirectory(k_TestDirectory);
        m_LocalTriggers = new TriggerConfig[]
        {
            new ("00000000-0000-0000-0000-000000000001", "Trigger1", "EventType1", "ActionType1", "ActionUrn1")
            {
                Path = Path.Combine(k_TestDirectory, "Trigger1.tr")
            }
        };

        m_RemoteTriggers = new TriggerConfig[]
        {
            new ("00000000-0000-0000-0000-000000000001", "Trigger1", "EventType1", "ActionType1", "ActionUrn1")
            {
                Name = "Trigger1",
                Path = Path.Combine(k_TestDirectory, "Trigger1.tr")
            },
            new ("00000000-0000-0000-0000-000000000002", "Trigger2", "EventType2", "ActionType2", "ActionUrn2")
            {
                Name = "Trigger2",
                Path = Path.Combine(k_TestDirectory, "Trigger2.tr")
            }
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

    static async Task CreateDeployFileAsync(IReadOnlyList<TriggerConfig> testCases)
    {
        var file = new TriggersConfigFile()
        {
            Configs = testCases
                .Select(c => new TriggerConfig(
                    c.Name, c.EventType, c.ActionType, c.ActionUrn))
                .ToList()
        };
        var serialized = JsonConvert.SerializeObject(file);

        await File.WriteAllTextAsync(Path.Combine(k_TestDirectory, "Trigger1.tr"), serialized);
    }

    [Test]
    public async Task FetchToValidConfigFromDirectorySucceeds()
    {
        var localTriggers = m_LocalTriggers!;
        await CreateDeployFileAsync(localTriggers);
        var expectedResult = new TriggersFetchResult(
                updated: new IDeploymentItem[]{ localTriggers[0] },
                deleted: Array.Empty<IDeploymentItem>(),
                created: Array.Empty<IDeploymentItem>(),
                authored: new IDeploymentItem[]{new TriggersFileItem(null!, localTriggers[0].Path)},
                failed: Array.Empty<IDeploymentItem>()
            );
        await GetFullySetCli()
            .DebugCommand($"fetch {k_TestDirectory} -s triggers")
            .AssertStandardOutputContains(expectedResult.ToString())
            .AssertNoErrors()
            .ExecuteAsync();
    }

    [Test]
    public async Task FetchToValidConfigFromDirectoryReconcileSucceeds()
    {
        var localTriggers = m_LocalTriggers!;
        await CreateDeployFileAsync(localTriggers);
        var expectedResult = new TriggersFetchResult(
            updated: new IDeploymentItem[]{ localTriggers[0] },
            deleted: Array.Empty<IDeploymentItem>(),
            created: new IDeploymentItem[]{ m_RemoteTriggers![1] },
            authored: new IDeploymentItem[]
            {
                new TriggersFileItem(null!, localTriggers[0].Path),
                new TriggersFileItem(null!, m_RemoteTriggers[1].Path)
            },
            failed: Array.Empty<IDeploymentItem>()
        );
        await GetFullySetCli()
            .DebugCommand($"fetch {k_TestDirectory} --reconcile -s triggers")
            .AssertStandardOutputContains(expectedResult.ToString())
            .AssertNoErrors()
            .ExecuteAsync();
    }

    [Test]
    public async Task FetchToValidConfigFromDirectoryDryRunSucceeds()
    {
        var localTriggers = m_LocalTriggers!;
        await CreateDeployFileAsync(localTriggers);
        var expectedResult = new TriggersFetchResult(
            updated: new IDeploymentItem[]{ localTriggers[0] },
            deleted: Array.Empty<IDeploymentItem>(),
            created: Array.Empty<IDeploymentItem>(),
            authored: new IDeploymentItem[]{new TriggersFileItem(null!, localTriggers[0].Path)},
            failed: Array.Empty<IDeploymentItem>(),
            dryRun: true
        );

        var ex = expectedResult.ToString();
        await GetFullySetCli()
            .DebugCommand($"fetch {k_TestDirectory} --dry-run -s triggers")
            .AssertStandardOutputContains(ex)
            .AssertNoErrors()
            .ExecuteAsync();
    }

    [Test]
    public async Task FetchToValidConfigFromDirectoryDryRunWithReconcileSucceeds()
    {
        var localTriggers = m_LocalTriggers!;
        await CreateDeployFileAsync(localTriggers);
        var expectedResult = new TriggersFetchResult(
            updated: new IDeploymentItem[]{ localTriggers[0] },
            deleted: Array.Empty<IDeploymentItem>(),
            created: new IDeploymentItem[]{ m_RemoteTriggers![1] },
            authored: new IDeploymentItem[]
            {
                new TriggersFileItem(null!, localTriggers[0].Path),
                new TriggersFileItem(null!, m_RemoteTriggers[1].Path)
            },
            failed: Array.Empty<IDeploymentItem>(),
            dryRun: true
        );
        await GetFullySetCli()
            .DebugCommand($"fetch {k_TestDirectory} --reconcile --dry-run -s triggers")
            .AssertStandardOutputContains(expectedResult.ToString())
            .AssertNoErrors()
            .ExecuteAsync();
    }

    [Test]
    public async Task FetchToValidConfigFromDuplicateIdFails()
    {
        var localTriggers = m_LocalTriggers!.Append(
            new TriggerConfig("00000000-0000-0000-0000-000000000001", "Trigger1", "EventType1", "ActionType1", "ActionUrn1")
            {
                Path = Path.Combine(k_TestDirectory, "Trigger1.tr")
            }).ToList();
        await CreateDeployFileAsync(localTriggers);

        foreach (var tr in localTriggers)
        {
            var failedMessage1 =
                DuplicateResourceValidation.GetDuplicateResourceErrorMessages(tr, localTriggers);
            var failedStatus = Statuses.GetFailedToFetch(failedMessage1.Item2);
            tr.Status = failedStatus;
        }

        await CreateDeployFileAsync(localTriggers);
        var expectedResult = new TriggersFetchResult(
            updated: Array.Empty<IDeploymentItem>(),
            deleted: Array.Empty<IDeploymentItem>(),
            created: Array.Empty<IDeploymentItem>(),
            authored: Array.Empty<IDeploymentItem>(),
            failed: new IDeploymentItem[]{ localTriggers[0], localTriggers[1] }
        );
        await GetFullySetCli()
            .DebugCommand($"fetch {k_TestDirectory} -s triggers")
            .AssertStandardOutputContains(expectedResult.ToString())
            .ExecuteAsync();
    }
}
