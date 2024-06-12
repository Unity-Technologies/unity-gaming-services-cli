using System.Threading.Tasks;
using Newtonsoft.Json;
using NUnit.Framework;
using Unity.Services.Cli.Common.Exceptions;
using Unity.Services.Cli.IntegrationTest.Common;
using Unity.Services.Cli.MockServer.Common;
using Unity.Services.Cli.MockServer.ServiceMocks;

namespace Unity.Services.Cli.IntegrationTest.CloudSaveTests;

public class CloudSaveTests : UgsCliFixture
{
    const string k_Default = "default";
    const string k_Private = "private";
    const string k_Public = "public";
    const string k_Protected = "protected";

    const string k_NotLoggedInOutput =
        " You are not logged into any service account. Please login using the 'ugs login' command.";

    const string k_CreateIndexBodyMissingOutput =
        "Required request body cannot be empty.";

    const string k_CreatePlayerIndexInvalidVisibilityOutput =
        "Valid options are: default, public, protected";

    const string k_CreateCustomIndexInvalidVisibilityOutput =
        "Valid options are: default, private";

    const string k_MissingProjectIdOutput = "'project-id' is not set in project configuration";
    const string k_MissingEnvironmentNameOutput = "'environment-name' is not set in project configuration";

    static readonly string k_ValidQueryIndexBody =
        "{\"fields\":[{\"op\":\"EQ\",\"key\":\"fieldFilter_key\",\"value\":\"fieldFilter_value\",\"asc\":true}],\"returnKeys\":[\"returnKey1\",\"returnKey2\"],\"offset\":5,\"limit\":10}";

    static readonly string k_ValidCreateIndexFields =
        "[{\"key\":\"key1\",\"asc\":true},{\"key\":\"key2\",\"asc\":false}]";

    static readonly string k_ValidCreateIndexBody =
        "{\"indexConfig\": {\"fields\":" + k_ValidCreateIndexFields + "}}";

    [SetUp]
    public async Task SetUp()
    {
        DeleteLocalConfig();
        DeleteLocalCredentials();

        await MockApi.MockServiceAsync(new CloudSaveApiMock());
        await MockApi.MockServiceAsync(new IdentityV1Mock());
    }

    [TearDown]
    public void TearDown()
    {
        DeleteLocalConfig();
        DeleteLocalCredentials();
        MockApi.Server?.ResetMappings();
    }

    [Test]
    public async Task CloudSave_ListIndexes_ThrowsWhenNotAuthenticated()
    {
        SetConfigValue("project-id", CommonKeys.ValidProjectId);
        SetConfigValue("environment-name", CommonKeys.ValidEnvironmentName);

        await new UgsCliTestCase()
            .Command($"cloud-save data index list")
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardErrorContains(k_NotLoggedInOutput)
            .ExecuteAsync();
    }

    [Test]
    public async Task CloudSave_ListIndexes_ThrowsWithProjectIdMissing()
    {
        SetConfigValue("environment-name", CommonKeys.ValidEnvironmentName);

        await new UgsCliTestCase()
            .Command($"cloud-save data index list")
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardErrorContains(k_MissingProjectIdOutput)
            .ExecuteAsync();
    }

    [Test]
    public async Task CloudSave_ListIndexes_ThrowsWithProjectIdEmpty()
    {
        SetConfigValue("environment-name", CommonKeys.ValidEnvironmentName);

        await new UgsCliTestCase()
            .Command($"cloud-save data index list --project-id \"\"")
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardErrorContains(k_MissingProjectIdOutput)
            .ExecuteAsync();
    }

    [Test]
    public async Task CloudSave_ListIndexes_ThrowsWithEnvironmentNameMissing()
    {
        SetConfigValue("project-id", CommonKeys.ValidProjectId);

        await new UgsCliTestCase()
            .Command($"cloud-save data index list")
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardErrorContains(k_MissingEnvironmentNameOutput)
            .ExecuteAsync();
    }

    [Test]
    public async Task CloudSave_ListIndexes_SucceedsWithValidInput()
    {
        SetConfigValue("project-id", CommonKeys.ValidProjectId);
        SetConfigValue("environment-name", CommonKeys.ValidEnvironmentName);

        await GetLoggedInCli()
            .Command($"cloud-save data index list")
            .AssertNoErrors()
            .DebugCommand("CloudSave_ListIndexes_SucceedsWithValidInput")
            .ExecuteAsync();
    }

    [Test]
    public async Task CloudSave_ListCustomIds_SucceedsWithValidInput()
    {
        SetConfigValue("project-id", CommonKeys.ValidProjectId);
        SetConfigValue("environment-name", CommonKeys.ValidEnvironmentName);

        await GetLoggedInCli()
            .Command($"cloud-save data custom list --limit 2 --start \"someId\"")
            .AssertNoErrors()
            .DebugCommand("CloudSave_ListCustomIds_SucceedsWithValidInput")
            .ExecuteAsync();
    }

    [Test]
    public async Task CloudSave_ListCustomIds_SucceedsWithNoInput()
    {
        SetConfigValue("project-id", CommonKeys.ValidProjectId);
        SetConfigValue("environment-name", CommonKeys.ValidEnvironmentName);

        await GetLoggedInCli()
            .Command($"cloud-save data custom list")
            .AssertNoErrors()
            .DebugCommand("CloudSave_ListCustomIds_SucceedsWithNoInput")
            .ExecuteAsync();
    }

    [Test]
    public async Task CloudSave_ListPlayerIds_SucceedsWithValidInput()
    {
        SetConfigValue("project-id", CommonKeys.ValidProjectId);
        SetConfigValue("environment-name", CommonKeys.ValidEnvironmentName);

        await GetLoggedInCli()
            .Command($"cloud-save data player list --limit 2 --start \"someId\"")
            .AssertNoErrors()
            .DebugCommand("CloudSave_ListPlayerIds_SucceedsWithValidInput")
            .ExecuteAsync();
    }

    [Test]
    public async Task CloudSave_ListPlayerIds_SucceedsWithNoInput()
    {
        SetConfigValue("project-id", CommonKeys.ValidProjectId);
        SetConfigValue("environment-name", CommonKeys.ValidEnvironmentName);

        await GetLoggedInCli()
            .Command($"cloud-save data player list")
            .AssertNoErrors()
            .DebugCommand("CloudSave_ListPlayerIds_SucceedsWithNoInput")
            .ExecuteAsync();
    }

    [Test]
    public async Task CloudSave_QueryPlayerData_Default_SucceedsWithValidInput()
    {
        SetConfigValue("project-id", CommonKeys.ValidProjectId);
        SetConfigValue("environment-name", CommonKeys.ValidEnvironmentName);

        await GetLoggedInCli()
            .Command($"cloud-save data player query --body \"{JsonConvert.SerializeObject(k_ValidQueryIndexBody)}\" --visibility {k_Default}")
            .AssertNoErrors()
            .ExecuteAsync();
    }

    [Test]
    public async Task CloudSave_QueryPlayerData_Default_NotSpecified_SucceedsWithValidInput()
    {
        SetConfigValue("project-id", CommonKeys.ValidProjectId);
        SetConfigValue("environment-name", CommonKeys.ValidEnvironmentName);

        await GetLoggedInCli()
            .Command($"cloud-save data player query --body \"{JsonConvert.SerializeObject(k_ValidQueryIndexBody)}\"")
            .AssertNoErrors()
            .ExecuteAsync();
    }

    [Test]
    public async Task CloudSave_QueryPlayerData_Protected_SucceedsWithValidInput()
    {
        SetConfigValue("project-id", CommonKeys.ValidProjectId);
        SetConfigValue("environment-name", CommonKeys.ValidEnvironmentName);

        await GetLoggedInCli()
            .Command($"cloud-save data player query --body \"{JsonConvert.SerializeObject(k_ValidQueryIndexBody)}\" --visibility {k_Protected}")
            .AssertNoErrors()
            .ExecuteAsync();
    }

    [Test]
    public async Task CloudSave_QueryPlayerData_Public_SucceedsWithValidInput()
    {
        SetConfigValue("project-id", CommonKeys.ValidProjectId);
        SetConfigValue("environment-name", CommonKeys.ValidEnvironmentName);

        await GetLoggedInCli()
            .Command($"cloud-save data player query --body \"{JsonConvert.SerializeObject(k_ValidQueryIndexBody)}\" --visibility {k_Protected}")
            .AssertNoErrors()
            .ExecuteAsync();
    }

    [Test]
    public async Task CloudSave_QueryCustomData_Default_SucceedsWithValidInput()
    {
        SetConfigValue("project-id", CommonKeys.ValidProjectId);
        SetConfigValue("environment-name", CommonKeys.ValidEnvironmentName);

        await GetLoggedInCli()
            .Command($"cloud-save data custom query --body \"{JsonConvert.SerializeObject(k_ValidQueryIndexBody)}\" --visibility {k_Default}")
            .AssertNoErrors()
            .ExecuteAsync();
    }

    [Test]
    public async Task CloudSave_QueryCustomData_NotSpecified_SucceedsWithValidInput()
    {
        SetConfigValue("project-id", CommonKeys.ValidProjectId);
        SetConfigValue("environment-name", CommonKeys.ValidEnvironmentName);

        await GetLoggedInCli()
            .Command($"cloud-save data custom query --body \"{JsonConvert.SerializeObject(k_ValidQueryIndexBody)}\"")
            .AssertNoErrors()
            .ExecuteAsync();
    }

    [Test]
    public async Task CloudSave_QueryCustomData_Private_SucceedsWithValidInput()
    {
        SetConfigValue("project-id", CommonKeys.ValidProjectId);
        SetConfigValue("environment-name", CommonKeys.ValidEnvironmentName);

        await GetLoggedInCli()
            .Command($"cloud-save data custom query --body \"{JsonConvert.SerializeObject(k_ValidQueryIndexBody)}\" --visibility {k_Private}")
            .AssertNoErrors()
            .ExecuteAsync();
    }

    [Test]
    public async Task CloudSave_CreatePlayerIndex_Default_SucceedsWithValidInput_UsingFields()
    {
        SetConfigValue("project-id", CommonKeys.ValidProjectId);
        SetConfigValue("environment-name", CommonKeys.ValidEnvironmentName);

        await GetLoggedInCli()
            .Command($"cloud-save data index player create --fields {JsonConvert.SerializeObject(k_ValidCreateIndexFields)} --visibility {k_Default}")
            .AssertNoErrors()
            .ExecuteAsync();
    }

    [Test]
    public async Task CloudSave_CreatePlayerIndex_Default_SucceedsWithValidInput_UsingBody()
    {
        SetConfigValue("project-id", CommonKeys.ValidProjectId);
        SetConfigValue("environment-name", CommonKeys.ValidEnvironmentName);

        await GetLoggedInCli()
            .Command($"cloud-save data index player create --body {JsonConvert.SerializeObject(k_ValidCreateIndexBody)} --visibility {k_Default}")
            .AssertNoErrors()
            .ExecuteAsync();
    }

    [Test]
    public async Task CloudSave_CreatePlayerIndex_Default_NotSpecified_SucceedsWithValidInput_UsingBody()
    {
        SetConfigValue("project-id", CommonKeys.ValidProjectId);
        SetConfigValue("environment-name", CommonKeys.ValidEnvironmentName);

        await GetLoggedInCli()
            .Command($"cloud-save data index player create --body {JsonConvert.SerializeObject(k_ValidCreateIndexBody)}")
            .AssertNoErrors()
            .ExecuteAsync();
    }

    [Test]
    public async Task CloudSave_CreatePlayerIndex_Default_NotSpecified_SucceedsWithValidInput_UsingFields()
    {
        SetConfigValue("project-id", CommonKeys.ValidProjectId);
        SetConfigValue("environment-name", CommonKeys.ValidEnvironmentName);

        await GetLoggedInCli()
            .Command($"cloud-save data index player create --fields {JsonConvert.SerializeObject(k_ValidCreateIndexFields)}")
            .AssertNoErrors()
            .ExecuteAsync();
    }

    [Test]
    public async Task CloudSave_CreatePlayerIndex_Public_SucceedsWithValidInput_UsingFields()
    {
        SetConfigValue("project-id", CommonKeys.ValidProjectId);
        SetConfigValue("environment-name", CommonKeys.ValidEnvironmentName);

        await GetLoggedInCli()
            .Command($"cloud-save data index player create --fields {JsonConvert.SerializeObject(k_ValidCreateIndexFields)} --visibility {k_Public}")
            .AssertNoErrors()
            .ExecuteAsync();
    }

    [Test]
    public async Task CloudSave_CreatePlayerIndex_Public_SucceedsWithValidInput_UsingBody()
    {
        SetConfigValue("project-id", CommonKeys.ValidProjectId);
        SetConfigValue("environment-name", CommonKeys.ValidEnvironmentName);

        await GetLoggedInCli()
            .Command($"cloud-save data index player create --body {JsonConvert.SerializeObject(k_ValidCreateIndexBody)} --visibility {k_Public}")
            .AssertNoErrors()
            .ExecuteAsync();
    }

    [Test]
    public async Task CloudSave_CreatePlayerIndex_Protected_SucceedsWithValidInput_UsingFields()
    {
        SetConfigValue("project-id", CommonKeys.ValidProjectId);
        SetConfigValue("environment-name", CommonKeys.ValidEnvironmentName);

        await GetLoggedInCli()
            .Command($"cloud-save data index player create --fields {JsonConvert.SerializeObject(k_ValidCreateIndexFields)} --visibility {k_Protected}")
            .AssertNoErrors()
            .ExecuteAsync();
    }

    [Test]
    public async Task CloudSave_CreatePlayerIndex_Protected_SucceedsWithValidInput_UsingBody()
    {
        SetConfigValue("project-id", CommonKeys.ValidProjectId);
        SetConfigValue("environment-name", CommonKeys.ValidEnvironmentName);

        await GetLoggedInCli()
            .Command($"cloud-save data index player create --body {JsonConvert.SerializeObject(k_ValidCreateIndexBody)} --visibility {k_Protected}")
            .AssertNoErrors()
            .ExecuteAsync();
    }

    [Test]
    public async Task CloudSave_CreatePlayerIndex_ThrowsWhenNotAuthenticated()
    {
        SetConfigValue("project-id", CommonKeys.ValidProjectId);
        SetConfigValue("environment-name", CommonKeys.ValidEnvironmentName);

        await new UgsCliTestCase()
            .Command($"cloud-save data index player create --visibility {k_Default}")
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardErrorContains(k_NotLoggedInOutput)
            .ExecuteAsync();
    }

    [Test]
    public async Task CloudSave_CreatePlayerIndex_ThrowsWithProjectIdMissing()
    {
        SetConfigValue("environment-name", CommonKeys.ValidEnvironmentName);

        await new UgsCliTestCase()
            .Command($"cloud-save data index player create --visibility {k_Default}")
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardErrorContains(k_MissingProjectIdOutput)
            .ExecuteAsync();
    }

    [Test]
    public async Task CloudSave_CreatePlayerIndex_ThrowsWithEnvironmentNameMissing()
    {
        SetConfigValue("project-id", CommonKeys.ValidProjectId);

        await new UgsCliTestCase()
            .Command($"cloud-save data index player create --visibility {k_Default}")
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardErrorContains(k_MissingEnvironmentNameOutput)
            .ExecuteAsync();
    }

    [Test]
    public async Task CloudSave_CreatePlayerIndex_ThrowsWithProjectIdEmpty()
    {
        SetConfigValue("environment-name", CommonKeys.ValidEnvironmentName);

        await new UgsCliTestCase()
            .Command($"cloud-save data index player create --visibility {k_Default}")
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardErrorContains(k_MissingProjectIdOutput)
            .ExecuteAsync();
    }

    [Test]
    public async Task CloudSave_CreatePlayerIndex_ThrowsWithBodyMissing()
    {
        SetConfigValue("project-id", CommonKeys.ValidProjectId);
        SetConfigValue("environment-name", CommonKeys.ValidEnvironmentName);

        await GetLoggedInCli()
            .Command($"cloud-save data index player create --visibility {k_Default}")
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardErrorContains(k_CreateIndexBodyMissingOutput)
            .ExecuteAsync();
    }

    [Test]
    public async Task CloudSave_CreatePlayerIndex_ThrowsWithInvalidVisibility()
    {
        SetConfigValue("project-id", CommonKeys.ValidProjectId);
        SetConfigValue("environment-name", CommonKeys.ValidEnvironmentName);

        string invalidVisibility = "invalidVisibility";

        await GetLoggedInCli()
            .Command($"cloud-save data index player create --fields {JsonConvert.SerializeObject(k_ValidCreateIndexFields)} --visibility {invalidVisibility}")
            .AssertStandardErrorContains(k_CreatePlayerIndexInvalidVisibilityOutput)
            .ExecuteAsync();
    }

    [Test]
    public async Task CloudSave_CreateCustomIndex_Default_SucceedsWithValidInput_UsingFields()
    {
        SetConfigValue("project-id", CommonKeys.ValidProjectId);
        SetConfigValue("environment-name", CommonKeys.ValidEnvironmentName);

        await GetLoggedInCli()
            .Command($"cloud-save data index custom create --fields {JsonConvert.SerializeObject(k_ValidCreateIndexFields)} --visibility {k_Default}")
            .AssertNoErrors()
            .ExecuteAsync();
    }

    [Test]
    public async Task CloudSave_CreateCustomIndex_Default_SucceedsWithValidInput_UsingBody()
    {
        SetConfigValue("project-id", CommonKeys.ValidProjectId);
        SetConfigValue("environment-name", CommonKeys.ValidEnvironmentName);

        await GetLoggedInCli()
            .Command($"cloud-save data index custom create --body {JsonConvert.SerializeObject(k_ValidCreateIndexBody)} --visibility {k_Default}")
            .AssertNoErrors()
            .ExecuteAsync();
    }

    [Test]
    public async Task CloudSave_CreateCustomIndex_Default_NotSpecified_SucceedsWithValidInput_UsingBody()
    {
        SetConfigValue("project-id", CommonKeys.ValidProjectId);
        SetConfigValue("environment-name", CommonKeys.ValidEnvironmentName);

        await GetLoggedInCli()
            .Command($"cloud-save data index custom create --body {JsonConvert.SerializeObject(k_ValidCreateIndexBody)}")
            .AssertNoErrors()
            .ExecuteAsync();
    }

    [Test]
    public async Task CloudSave_CreateCustomIndex_Default_NotSpecified_SucceedsWithValidInput_UsingFields()
    {
        SetConfigValue("project-id", CommonKeys.ValidProjectId);
        SetConfigValue("environment-name", CommonKeys.ValidEnvironmentName);

        await GetLoggedInCli()
            .Command($"cloud-save data index custom create --fields {JsonConvert.SerializeObject(k_ValidCreateIndexFields)}")
            .AssertNoErrors()
            .ExecuteAsync();
    }

    [Test]
    public async Task CloudSave_CreateCustomIndex_Private_SucceedsWithValidInput_UsingFields()
    {
        SetConfigValue("project-id", CommonKeys.ValidProjectId);
        SetConfigValue("environment-name", CommonKeys.ValidEnvironmentName);

        await GetLoggedInCli()
            .Command($"cloud-save data index custom create --fields {JsonConvert.SerializeObject(k_ValidCreateIndexFields)} --visibility {k_Private}")
            .AssertNoErrors()
            .ExecuteAsync();
    }

    [Test]
    public async Task CloudSave_CreateCustomIndex_Private_SucceedsWithValidInput_UsingBody()
    {
        SetConfigValue("project-id", CommonKeys.ValidProjectId);
        SetConfigValue("environment-name", CommonKeys.ValidEnvironmentName);

        await GetLoggedInCli()
            .Command($"cloud-save data index custom create --body {JsonConvert.SerializeObject(k_ValidCreateIndexBody)} --visibility {k_Private}")
            .AssertNoErrors()
            .ExecuteAsync();
    }

    [Test]
    public async Task CloudSave_CreateCustomIndex_ThrowsWhenNotAuthenticated()
    {
        SetConfigValue("project-id", CommonKeys.ValidProjectId);
        SetConfigValue("environment-name", CommonKeys.ValidEnvironmentName);

        await new UgsCliTestCase()
            .Command($"cloud-save data index custom create --visibility {k_Default}")
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardErrorContains(k_NotLoggedInOutput)
            .ExecuteAsync();
    }

    [Test]
    public async Task CloudSave_CreateCustomIndex_ThrowsWithProjectIdMissing()
    {
        SetConfigValue("environment-name", CommonKeys.ValidEnvironmentName);

        await new UgsCliTestCase()
            .Command($"cloud-save data index custom create --visibility {k_Default}")
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardErrorContains(k_MissingProjectIdOutput)
            .ExecuteAsync();
    }

    [Test]
    public async Task CloudSave_CreateCustomIndex_ThrowsWithEnvironmentNameMissing()
    {
        SetConfigValue("project-id", CommonKeys.ValidProjectId);

        await new UgsCliTestCase()
            .Command($"cloud-save data index custom create --visibility {k_Default}")
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardErrorContains(k_MissingEnvironmentNameOutput)
            .ExecuteAsync();
    }

    [Test]
    public async Task CloudSave_CreateCustomIndex_ThrowsWithProjectIdEmpty()
    {
        SetConfigValue("environment-name", CommonKeys.ValidEnvironmentName);

        await new UgsCliTestCase()
            .Command($"cloud-save data index custom create --visibility {k_Default}")
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardErrorContains(k_MissingProjectIdOutput)
            .ExecuteAsync();
    }

    [Test]
    public async Task CloudSave_CreateCustomIndex_ThrowsWithBodyMissing()
    {
        SetConfigValue("project-id", CommonKeys.ValidProjectId);
        SetConfigValue("environment-name", CommonKeys.ValidEnvironmentName);

        await GetLoggedInCli()
            .Command($"cloud-save data index custom create --visibility {k_Default}")
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardErrorContains(k_CreateIndexBodyMissingOutput)
            .ExecuteAsync();
    }

    [Test]
    public async Task CloudSave_CreateCustomIndex_ThrowsWithInvalidVisibility()
    {
        SetConfigValue("project-id", CommonKeys.ValidProjectId);
        SetConfigValue("environment-name", CommonKeys.ValidEnvironmentName);

        string invalidVisibility = "invalidVisibility";

        await GetLoggedInCli()
            .Command($"cloud-save data index custom create --fields {JsonConvert.SerializeObject(k_ValidCreateIndexFields)} --visibility {invalidVisibility}")
            .AssertStandardErrorContains(k_CreateCustomIndexInvalidVisibilityOutput)
            .ExecuteAsync();
    }
}
