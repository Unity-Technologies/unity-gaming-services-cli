using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NUnit.Framework;
using Unity.Services.Cli.Common.Exceptions;
using Unity.Services.Cli.IntegrationTest.Common;
using Unity.Services.Cli.MockServer.Common;
using Unity.Services.Cli.MockServer.ServiceMocks.GameServerHosting;

namespace Unity.Services.Cli.IntegrationTest.GameServerHostingTests;

public partial class GameServerHostingTests
{
    static readonly string k_ValidUsageSettingJson = JsonConvert.SerializeObject(Keys.ValidUsageSettingsJson);
    static readonly string k_FleetCreateCommand =
        $"mh fleet create --name test --os-family linux --region-id {Keys.ValidTemplateRegionId} --build-configuration-id {Keys.ValidBuildConfigurationId} --usage-setting {k_ValidUsageSettingJson}";

    [Test]
    [Category("mh")]
    [Category("mh fleet")]
    [Category("mh fleet create")]
    public async Task FleetCreate_ThrowsNotLoggedInException()
    {
        SetConfigValue("project-id", CommonKeys.ValidProjectId);
        SetConfigValue("environment-id", CommonKeys.ValidEnvironmentId);
        SetConfigValue("environment-name", CommonKeys.ValidEnvironmentName);

        await new UgsCliTestCase()
            .Command(k_FleetCreateCommand)
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardErrorContains(k_NotLoggedIn)
            .ExecuteAsync();
    }

    [Test]
    [Category("mh")]
    [Category("mh fleet")]
    [Category("mh fleet create")]
    public async Task FleetCreate_ThrowsProjectIdNotSetException()
    {
        SetConfigValue("environment-id", CommonKeys.ValidEnvironmentId);
        SetConfigValue("environment-name", CommonKeys.ValidEnvironmentName);

        await GetLoggedInCli()
            .Command(k_FleetCreateCommand)
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardErrorContains(k_ProjectIdIsNotSet)
            .ExecuteAsync();
    }

    [Test]
    [Category("mh")]
    [Category("mh fleet")]
    [Category("mh fleet create")]
    public async Task FleetCreate_ThrowsEnvironmentNameNotSetException()
    {
        SetConfigValue("project-id", CommonKeys.ValidProjectId);
        SetConfigValue("environment-id", CommonKeys.ValidEnvironmentId);

        await GetLoggedInCli()
            .Command(k_FleetCreateCommand)
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardErrorContains(k_EnvironmentNameIsNotSet)
            .ExecuteAsync();
    }

    [Test]
    [Category("mh")]
    [Category("mh fleet")]
    [Category("mh fleet create")]
    public async Task FleetCreate_ThrowsBuildConfigurationIdNotSetException()
    {
        await GetFullySetCli()
            .Command($"mh fleet create --name test --os-family linux --region-id {Keys.ValidTemplateRegionId}")
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardErrorContains("Option '--build-configuration-id' is required.")
            .ExecuteAsync();
    }

    [Test]
    [Category("mh")]
    [Category("mh fleet")]
    [Category("mh fleet create")]
    public async Task FleetCreate_ThrowsRegionIdNotSetException()
    {
        await GetFullySetCli()
            .Command(
                $"mh fleet create --name test --os-family linux --build-configuration-id {Keys.ValidBuildConfigurationId}")
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardErrorContains("Option '--region-id' is required.")
            .ExecuteAsync();
    }

    [Test]
    [Category("mh")]
    [Category("mh fleet")]
    [Category("mh fleet create")]
    public async Task FleetCreate_ThrowsOsFamilyNotSetException()
    {
        await GetFullySetCli()
            .Command(
                $"mh fleet create --name test --region-id {Keys.ValidTemplateRegionId} --build-configuration-id {Keys.ValidBuildConfigurationId}")
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardErrorContains("Option '--os-family' is required.")
            .ExecuteAsync();
    }

    [Test]
    [Category("mh")]
    [Category("mh fleet")]
    [Category("mh fleet create")]
    public async Task FleetCreate_ThrowsNameNotSetException()
    {
        await GetFullySetCli()
            .Command(
                $"mh fleet create --os-family linux --region-id {Keys.ValidTemplateRegionId} --build-configuration-id {Keys.ValidBuildConfigurationId}")
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardErrorContains("Option '--name' is required.")
            .ExecuteAsync();
    }

    [Test]
    [Category("mh")]
    [Category("mh fleet")]
    [Category("mh fleet create")]
    public async Task FleetCreate_ThrowsInvalidBuildConfigurationIdException()
    {
        await GetFullySetCli()
            .Command(
                $"mh fleet create --name test --os-family linux --region-id {Keys.ValidTemplateRegionId} --build-configuration-id invalid")
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardErrorContains(
                "Cannot parse argument 'invalid' for option '--build-configuration-id' as expected type 'System.Int64'.")
            .ExecuteAsync();
    }

    [Test]
    [Category("mh")]
    [Category("mh fleet")]
    [Category("mh fleet create")]
    public async Task FleetCreate_ThrowsInvalidRegionIdException()
    {
        await GetFullySetCli()
            .Command(
                $"mh fleet create --name test --os-family linux --region-id invalid --build-configuration-id {Keys.ValidBuildConfigurationId}")
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardErrorContains("Region 'invalid' not a valid UUID.")
            .ExecuteAsync();
    }

    [Test]
    [Category("mh")]
    [Category("mh fleet")]
    [Category("mh fleet create")]
    public async Task FleetCreate_ThrowsInvalidOsFamilyException()
    {
        await GetFullySetCli()
            .Command(
                $"mh fleet create --name test --os-family invalid --region-id {Keys.ValidTemplateRegionId} --build-configuration-id {Keys.ValidBuildConfigurationId}")
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardErrorContains("Invalid option for --os-family.")
            .ExecuteAsync();
    }

    [Test]
    [Category("mh")]
    [Category("mh fleet")]
    [Category("mh fleet create")]
    public async Task FleetCreate_ThrowsInvalidUsageSettingsJsonException()
    {
        await GetFullySetCli()
            .Command(
                $"mh fleet create --name test --os-family linux --region-id {Keys.ValidTemplateRegionId} --build-configuration-id {Keys.ValidBuildConfigurationId} --usage-setting invalid_json")
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardErrorContains("Invalid option for --usage-setting")
            .ExecuteAsync();
    }

    [Test]
    [Category("mh")]
    [Category("mh fleet")]
    [Category("mh fleet create")]
    public async Task FleetCreate_SucceedsWithValidInput()
    {
        await GetFullySetCli()
            .Command(k_FleetCreateCommand)
            .AssertNoErrors()
            .ExecuteAsync();
    }

    [Test]
    [Category("mh")]
    [Category("mh fleet")]
    [Category("mh fleet delete")]
    public async Task FleetDelete_ThrowsNotLoggedInException()
    {
        SetConfigValue("project-id", CommonKeys.ValidProjectId);
        SetConfigValue("environment-id", CommonKeys.ValidEnvironmentId);
        SetConfigValue("environment-name", CommonKeys.ValidEnvironmentName);

        await new UgsCliTestCase()
            .Command($"mh fleet delete {Keys.ValidFleetId}")
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardErrorContains(k_NotLoggedIn)
            .ExecuteAsync();
    }

    [Test]
    [Category("mh")]
    [Category("mh fleet")]
    [Category("mh fleet delete")]
    public async Task FleetDelete_ThrowsProjectIdNotSetException()
    {
        SetConfigValue("environment-id", CommonKeys.ValidEnvironmentId);
        SetConfigValue("environment-name", CommonKeys.ValidEnvironmentName);

        await GetLoggedInCli()
            .Command($"mh fleet delete {Keys.ValidFleetId}")
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardErrorContains(k_ProjectIdIsNotSet)
            .ExecuteAsync();
    }

    [Test]
    [Category("mh")]
    [Category("mh fleet")]
    [Category("mh fleet delete")]
    public async Task FleetDelete_ThrowsEnvironmentNameNotSetException()
    {
        SetConfigValue("project-id", CommonKeys.ValidProjectId);

        await GetLoggedInCli()
            .Command($"mh fleet delete {Keys.ValidFleetId}")
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardErrorContains(k_EnvironmentNameIsNotSet)
            .ExecuteAsync();
    }

    [Test]
    [Category("mh")]
    [Category("mh fleet")]
    [Category("mh fleet delete")]
    public async Task FleetDelete_ThrowsFleetIdNotValidException()
    {
        await GetFullySetCli()
            .Command("mh fleet delete invalid-fleet-id")
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardErrorContains("Fleet 'invalid-fleet-id' not a valid UUID.")
            .ExecuteAsync();
    }

    [Test]
    [Category("mh")]
    [Category("mh fleet")]
    [Category("mh fleet delete")]
    public async Task FleetDelete_ThrowsFleetIdNotSetException()
    {
        await GetFullySetCli()
            .Command("mh fleet delete")
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardErrorContains("Required argument missing for command: 'delete'.")
            .ExecuteAsync();
    }

    [Test]
    [Category("mh")]
    [Category("mh fleet")]
    [Category("mh fleet delete")]
    public async Task FleetDelete_SucceedsWithValidInput()
    {
        await GetFullySetCli()
            .Command($"mh fleet delete {Keys.ValidFleetId}")
            .AssertExitCode(ExitCode.Success)
            .AssertStandardOutputContains("Deleting fleet...")
            .AssertStandardErrorContains("Fleet deleted successfully")
            .ExecuteAsync();
    }

    [Test]
    [Category("mh")]
    [Category("mh fleet")]
    [Category("mh fleet get")]
    public async Task FleetGet_ThrowsNotLoggedInException()
    {
        SetConfigValue("project-id", CommonKeys.ValidProjectId);
        SetConfigValue("environment-id", CommonKeys.ValidEnvironmentId);
        SetConfigValue("environment-name", CommonKeys.ValidEnvironmentName);

        await new UgsCliTestCase()
            .Command($"mh fleet get {Keys.ValidFleetId}")
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardErrorContains(k_NotLoggedIn)
            .ExecuteAsync();
    }

    [Test]
    [Category("mh")]
    [Category("mh fleet")]
    [Category("mh fleet get")]
    public async Task FleetGet_ThrowsProjectIdNotSetException()
    {
        SetConfigValue("environment-id", CommonKeys.ValidEnvironmentId);
        SetConfigValue("environment-name", CommonKeys.ValidEnvironmentName);

        await GetLoggedInCli()
            .Command($"mh fleet get {Keys.ValidFleetId}")
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardErrorContains(k_ProjectIdIsNotSet)
            .ExecuteAsync();
    }

    [Test]
    [Category("mh")]
    [Category("mh fleet")]
    [Category("mh fleet get")]
    public async Task FleetGet_ThrowsEnvironmentNameNotSetException()
    {
        SetConfigValue("project-id", CommonKeys.ValidProjectId);

        await GetLoggedInCli()
            .Command($"mh fleet get {Keys.ValidFleetId}")
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardErrorContains(k_EnvironmentNameIsNotSet)
            .ExecuteAsync();
    }

    [Test]
    [Category("mh")]
    [Category("mh fleet")]
    [Category("mh fleet get")]
    public async Task FleetGet_ThrowsFleetIdNotValidException()
    {
        await GetFullySetCli()
            .Command("mh fleet get invalid-fleet-id")
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardErrorContains("Fleet 'invalid-fleet-id' not a valid UUID.")
            .ExecuteAsync();
    }

    [Test]
    [Category("mh")]
    [Category("mh fleet")]
    [Category("mh fleet get")]
    public async Task FleetGet_ThrowsFleetIdNotSetException()
    {
        await GetFullySetCli()
            .Command("mh fleet get")
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardErrorContains("Required argument missing for command: 'get'.")
            .ExecuteAsync();
    }

    [Test]
    [Category("mh")]
    [Category("mh fleet")]
    [Category("mh fleet get")]
    public async Task FleetGet_SucceedsWithValidInput()
    {
        await GetFullySetCli()
            .Command($"mh fleet get {Keys.ValidFleetId}")
            .AssertNoErrors()
            .AssertStandardOutputContains("name: Test Fleet")
            .ExecuteAsync();
    }

    [Test]
    [Category("mh")]
    [Category("mh fleet")]
    [Category("mh fleet list")]
    public async Task FleetList_ThrowsNotLoggedInException()
    {
        SetConfigValue("project-id", CommonKeys.ValidProjectId);
        SetConfigValue("environment-id", CommonKeys.ValidEnvironmentId);
        SetConfigValue("environment-name", CommonKeys.ValidEnvironmentName);

        await new UgsCliTestCase()
            .Command("mh fleet list")
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardErrorContains(k_NotLoggedIn)
            .ExecuteAsync();
    }

    [Test]
    [Category("mh")]
    [Category("mh fleet")]
    [Category("mh fleet list")]
    public async Task FleetList_ThrowsProjectIdNotSetException()
    {
        SetConfigValue("environment-id", CommonKeys.ValidEnvironmentId);
        SetConfigValue("environment-name", CommonKeys.ValidEnvironmentName);

        await GetLoggedInCli()
            .Command("mh fleet list")
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardErrorContains(k_ProjectIdIsNotSet)
            .ExecuteAsync();
    }

    [Test]
    [Category("mh")]
    [Category("mh fleet")]
    [Category("mh fleet list")]
    public async Task FleetList_ThrowsEnvironmentNameNotSetException()
    {
        SetConfigValue("project-id", CommonKeys.ValidProjectId);

        await GetLoggedInCli()
            .Command("mh fleet list")
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardErrorContains(k_EnvironmentNameIsNotSet)
            .ExecuteAsync();
    }

    [Test]
    [Category("mh")]
    [Category("mh fleet")]
    [Category("mh fleet list")]
    public async Task FleetList_SucceedsWithValidInput()
    {
        await GetFullySetCli()
            .Command("mh fleet list")
            .AssertNoErrors()
            .AssertStandardOutputContains("Fetching fleet list...")
            .ExecuteAsync();
    }

    [Test]
    [Category("mh")]
    [Category("mh fleet")]
    [Category("mh fleet update")]
    public async Task FleetUpdate_ThrowsNotLoggedInException()
    {
        SetConfigValue("project-id", CommonKeys.ValidProjectId);
        SetConfigValue("environment-id", CommonKeys.ValidEnvironmentId);
        SetConfigValue("environment-name", CommonKeys.ValidEnvironmentName);

        await new UgsCliTestCase()
            .Command($"mh fleet update {Keys.ValidFleetId}")
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardErrorContains(k_NotLoggedIn)
            .ExecuteAsync();
    }

    [Test]
    [Category("mh")]
    [Category("mh fleet")]
    [Category("mh fleet update")]
    public async Task FleetUpdate_ThrowsProjectIdNotSetException()
    {
        SetConfigValue("environment-id", CommonKeys.ValidEnvironmentId);
        SetConfigValue("environment-name", CommonKeys.ValidEnvironmentName);

        await GetLoggedInCli()
            .Command($"mh fleet update {Keys.ValidFleetId}")
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardErrorContains(k_ProjectIdIsNotSet)
            .ExecuteAsync();
    }

    [Test]
    [Category("mh")]
    [Category("mh fleet")]
    [Category("mh fleet update")]
    public async Task FleetUpdate_ThrowsEnvironmentNameNotSetException()
    {
        SetConfigValue("project-id", CommonKeys.ValidProjectId);
        SetConfigValue("environment-id", CommonKeys.ValidEnvironmentId);

        await GetLoggedInCli()
            .Command($"mh fleet update {Keys.ValidFleetId}")
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardErrorContains(k_EnvironmentNameIsNotSet)
            .ExecuteAsync();
    }

    [Test]
    [Category("mh")]
    [Category("mh fleet")]
    [Category("mh fleet update")]
    public async Task FleetUpdate_ThrowsFleetIdNotValidException()
    {
        await GetFullySetCli()
            .Command("mh fleet update invalid-fleet-id")
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardErrorContains("Fleet 'invalid-fleet-id' not a valid UUID.")
            .ExecuteAsync();
    }

    [Test]
    [Category("mh")]
    [Category("mh fleet")]
    [Category("mh fleet update")]
    public async Task FleetUpdate_ThrowsFleetIdNotSetException()
    {
        await GetFullySetCli()
            .Command("mh fleet update")
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardErrorContains("Required argument missing for command: 'update'.")
            .ExecuteAsync();
    }

    [Test]
    [Category("mh")]
    [Category("mh fleet")]
    [Category("mh fleet update")]
    public async Task FleetUpdate_ThrowsInvalidBuildConfigurationIdException()
    {
        await GetFullySetCli()
            .Command($"mh fleet update {Keys.ValidFleetId} --build-configurations invalid")
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardErrorContains(
                "Cannot parse argument 'invalid' for option '--build-configurations' as expected type 'System.Int64'.")
            .ExecuteAsync();
    }

    [Test]
    [Category("mh")]
    [Category("mh fleet")]
    [Category("mh fleet update")]
    public async Task FleetUpdate_SucceedsWithValidInput()
    {
        await GetFullySetCli()
            .Command($"mh fleet update {Keys.ValidFleetId} --name updated --usage-setting {k_ValidUsageSettingJson}")
            .AssertExitCode(ExitCode.Success)
            .AssertStandardOutputContains("Updating fleet...")
            .AssertStandardErrorContains("Fleet updated successfully")
            .ExecuteAsync();
    }
}
