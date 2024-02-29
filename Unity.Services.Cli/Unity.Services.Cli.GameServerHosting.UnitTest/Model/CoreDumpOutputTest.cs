using Unity.Services.Cli.GameServerHosting.Model;
using Unity.Services.Gateway.GameServerHostingApiV1.Generated.Model;

namespace Unity.Services.Cli.GameServerHosting.UnitTest.Model;

[TestFixture]
[TestOf(typeof(CoreDumpOutput))]
public class CoreDumpOutputTest
{
    static IEnumerable<TestCaseData> TestCases()
    {
        var fleetId = Guid.NewGuid();
        var updatedAt = DateTime.UtcNow;
        var credentials = new CredentialsForTheBucket
        {
            StorageBucket = "testBucket",
        };
        yield return new TestCaseData(
            new GetCoreDumpConfig200Response
            {
                StorageType = GetCoreDumpConfig200Response.StorageTypeEnum.Gcs,
                Credentials = credentials,
                FleetId = fleetId,
                State = GetCoreDumpConfig200Response.StateEnum.NUMBER_1,
                UpdatedAt = updatedAt
            },
            "gcs",
            new CoreDumpCredentialsOutput(credentials),
            fleetId,
            "enabled",
            updatedAt
        ).SetName("Core Dump Config should be created");
        yield return new TestCaseData(
            new GetCoreDumpConfig200Response
            {
                Credentials = credentials,
                FleetId = fleetId,
                State = GetCoreDumpConfig200Response.StateEnum.NUMBER_1,
                UpdatedAt = updatedAt
            },
            "gcs",
            new CoreDumpCredentialsOutput(credentials),
            fleetId,
            "enabled",
            updatedAt
        ).SetName("unknown storage");
    }

    [TestCaseSource(nameof(TestCases))]
    public void CoreDumpOutput(
        GetCoreDumpConfig200Response response,
        string storageType,
        CoreDumpCredentialsOutput credentials,
        Guid fleetId,
        string state,
        DateTime updatedAt)
    {
        var actual = new CoreDumpOutput(response);

        Assert.Multiple(
            () =>
            {
                Assert.That(actual.StorageType, Is.EqualTo(storageType));
                Assert.That(actual.Credentials.StorageBucket, Is.EqualTo(credentials.StorageBucket));
                Assert.That(actual.FleetId, Is.EqualTo(fleetId));
                Assert.That(actual.State, Is.EqualTo(state));
                Assert.That(actual.UpdatedAt, Is.EqualTo(updatedAt));
            });
    }
}
