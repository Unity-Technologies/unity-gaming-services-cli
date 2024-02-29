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
        $"gsh fleet create --name test --os-family linux --region-id {Keys.ValidTemplateRegionId} --build-configuration-id {Keys.ValidBuildConfigurationId} --usage-setting {k_ValidUsageSettingJson}";

    [Test]
    [Category("gsh")]
    [Category("gsh fleet")]
    [Category("gsh fleet create")]
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
    [Category("gsh")]
    [Category("gsh fleet")]
    [Category("gsh fleet create")]
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
    [Category("gsh")]
    [Category("gsh fleet")]
    [Category("gsh fleet create")]
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
    [Category("gsh")]
    [Category("gsh fleet")]
    [Category("gsh fleet create")]
    public async Task FleetCreate_ThrowsBuildConfigurationIdNotSetException()
    {
        await GetFullySetCli()
            .Command($"gsh fleet create --name test --os-family linux --region-id {Keys.ValidTemplateRegionId}")
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardErrorContains("Option '--build-configuration-id' is required.")
            .ExecuteAsync();
    }

    [Test]
    [Category("gsh")]
    [Category("gsh fleet")]
    [Category("gsh fleet create")]
    public async Task FleetCreate_ThrowsRegionIdNotSetException()
    {
        await GetFullySetCli()
            .Command(
                $"gsh fleet create --name test --os-family linux --build-configuration-id {Keys.ValidBuildConfigurationId}")
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardErrorContains("Option '--region-id' is required.")
            .ExecuteAsync();
    }

    [Test]
    [Category("gsh")]
    [Category("gsh fleet")]
    [Category("gsh fleet create")]
    public async Task FleetCreate_ThrowsOsFamilyNotSetException()
    {
        await GetFullySetCli()
            .Command(
                $"gsh fleet create --name test --region-id {Keys.ValidTemplateRegionId} --build-configuration-id {Keys.ValidBuildConfigurationId}")
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardErrorContains("Option '--os-family' is required.")
            .ExecuteAsync();
    }

    [Test]
    [Category("gsh")]
    [Category("gsh fleet")]
    [Category("gsh fleet create")]
    public async Task FleetCreate_ThrowsNameNotSetException()
    {
        await GetFullySetCli()
            .Command(
                $"gsh fleet create --os-family linux --region-id {Keys.ValidTemplateRegionId} --build-configuration-id {Keys.ValidBuildConfigurationId}")
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardErrorContains("Option '--name' is required.")
            .ExecuteAsync();
    }

    [Test]
    [Category("gsh")]
    [Category("gsh fleet")]
    [Category("gsh fleet create")]
    public async Task FleetCreate_ThrowsInvalidBuildConfigurationIdException()
    {
        await GetFullySetCli()
            .Command(
                $"gsh fleet create --name test --os-family linux --region-id {Keys.ValidTemplateRegionId} --build-configuration-id invalid")
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardErrorContains(
                "Cannot parse argument 'invalid' for option '--build-configuration-id' as expected type 'System.Int64'.")
            .ExecuteAsync();
    }

    [Test]
    [Category("gsh")]
    [Category("gsh fleet")]
    [Category("gsh fleet create")]
    public async Task FleetCreate_ThrowsInvalidRegionIdException()
    {
        await GetFullySetCli()
            .Command(
                $"gsh fleet create --name test --os-family linux --region-id invalid --build-configuration-id {Keys.ValidBuildConfigurationId}")
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardErrorContains("Region 'invalid' not a valid UUID.")
            .ExecuteAsync();
    }

    [Test]
    [Category("gsh")]
    [Category("gsh fleet")]
    [Category("gsh fleet create")]
    public async Task FleetCreate_ThrowsInvalidOsFamilyException()
    {
        await GetFullySetCli()
            .Command(
                $"gsh fleet create --name test --os-family invalid --region-id {Keys.ValidTemplateRegionId} --build-configuration-id {Keys.ValidBuildConfigurationId}")
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardErrorContains("Invalid option for --os-family.")
            .ExecuteAsync();
    }

    [Test]
    [Category("gsh")]
    [Category("gsh fleet")]
    [Category("gsh fleet create")]
    public async Task FleetCreate_ThrowsInvalidUsageSettingsJsonException()
    {
        await GetFullySetCli()
            .Command(
                $"gsh fleet create --name test --os-family linux --region-id {Keys.ValidTemplateRegionId} --build-configuration-id {Keys.ValidBuildConfigurationId} --usage-setting invalid_json")
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardErrorContains("Invalid option for --usage-setting")
            .ExecuteAsync();
    }

    [Test]
    [Category("gsh")]
    [Category("gsh fleet")]
    [Category("gsh fleet create")]
    public async Task FleetCreate_SucceedsWithValidInput()
    {
        await GetFullySetCli()
            .Command(k_FleetCreateCommand)
            .AssertNoErrors()
            .ExecuteAsync();
    }

    [Test]
    [Category("gsh")]
    [Category("gsh fleet")]
    [Category("gsh fleet delete")]
    public async Task FleetDelete_ThrowsNotLoggedInException()
    {
        SetConfigValue("project-id", CommonKeys.ValidProjectId);
        SetConfigValue("environment-id", CommonKeys.ValidEnvironmentId);
        SetConfigValue("environment-name", CommonKeys.ValidEnvironmentName);

        await new UgsCliTestCase()
            .Command($"gsh fleet delete {Keys.ValidFleetId}")
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardErrorContains(k_NotLoggedIn)
            .ExecuteAsync();
    }

    [Test]
    [Category("gsh")]
    [Category("gsh fleet")]
    [Category("gsh fleet delete")]
    public async Task FleetDelete_ThrowsProjectIdNotSetException()
    {
        SetConfigValue("environment-id", CommonKeys.ValidEnvironmentId);
        SetConfigValue("environment-name", CommonKeys.ValidEnvironmentName);

        await GetLoggedInCli()
            .Command($"gsh fleet delete {Keys.ValidFleetId}")
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardErrorContains(k_ProjectIdIsNotSet)
            .ExecuteAsync();
    }

    [Test]
    [Category("gsh")]
    [Category("gsh fleet")]
    [Category("gsh fleet delete")]
    public async Task FleetDelete_ThrowsEnvironmentNameNotSetException()
    {
        SetConfigValue("project-id", CommonKeys.ValidProjectId);

        await GetLoggedInCli()
            .Command($"gsh fleet delete {Keys.ValidFleetId}")
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardErrorContains(k_EnvironmentNameIsNotSet)
            .ExecuteAsync();
    }

    [Test]
    [Category("gsh")]
    [Category("gsh fleet")]
    [Category("gsh fleet delete")]
    public async Task FleetDelete_ThrowsFleetIdNotValidException()
    {
        await GetFullySetCli()
            .Command("gsh fleet delete invalid-fleet-id")
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardErrorContains("Fleet 'invalid-fleet-id' not a valid UUID.")
            .ExecuteAsync();
    }

    [Test]
    [Category("gsh")]
    [Category("gsh fleet")]
    [Category("gsh fleet delete")]
    public async Task FleetDelete_ThrowsFleetIdNotSetException()
    {
        await GetFullySetCli()
            .Command("gsh fleet delete")
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardErrorContains("Required argument missing for command: 'delete'.")
            .ExecuteAsync();
    }

    [Test]
    [Category("gsh")]
    [Category("gsh fleet")]
    [Category("gsh fleet delete")]
    public async Task FleetDelete_SucceedsWithValidInput()
    {
        await GetFullySetCli()
            .Command($"gsh fleet delete {Keys.ValidFleetId}")
            .AssertExitCode(ExitCode.Success)
            .AssertStandardOutputContains("Deleting fleet...")
            .AssertStandardErrorContains("Fleet deleted successfully")
            .ExecuteAsync();
    }

    [Test]
    [Category("gsh")]
    [Category("gsh fleet")]
    [Category("gsh fleet get")]
    public async Task FleetGet_ThrowsNotLoggedInException()
    {
        SetConfigValue("project-id", CommonKeys.ValidProjectId);
        SetConfigValue("environment-id", CommonKeys.ValidEnvironmentId);
        SetConfigValue("environment-name", CommonKeys.ValidEnvironmentName);

        await new UgsCliTestCase()
            .Command($"gsh fleet get {Keys.ValidFleetId}")
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardErrorContains(k_NotLoggedIn)
            .ExecuteAsync();
    }

    [Test]
    [Category("gsh")]
    [Category("gsh fleet")]
    [Category("gsh fleet get")]
    public async Task FleetGet_ThrowsProjectIdNotSetException()
    {
        SetConfigValue("environment-id", CommonKeys.ValidEnvironmentId);
        SetConfigValue("environment-name", CommonKeys.ValidEnvironmentName);

        await GetLoggedInCli()
            .Command($"gsh fleet get {Keys.ValidFleetId}")
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardErrorContains(k_ProjectIdIsNotSet)
            .ExecuteAsync();
    }

    [Test]
    [Category("gsh")]
    [Category("gsh fleet")]
    [Category("gsh fleet get")]
    public async Task FleetGet_ThrowsEnvironmentNameNotSetException()
    {
        SetConfigValue("project-id", CommonKeys.ValidProjectId);

        await GetLoggedInCli()
            .Command($"gsh fleet get {Keys.ValidFleetId}")
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardErrorContains(k_EnvironmentNameIsNotSet)
            .ExecuteAsync();
    }

    [Test]
    [Category("gsh")]
    [Category("gsh fleet")]
    [Category("gsh fleet get")]
    public async Task FleetGet_ThrowsFleetIdNotValidException()
    {
        await GetFullySetCli()
            .Command("gsh fleet get invalid-fleet-id")
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardErrorContains("Fleet 'invalid-fleet-id' not a valid UUID.")
            .ExecuteAsync();
    }

    [Test]
    [Category("gsh")]
    [Category("gsh fleet")]
    [Category("gsh fleet get")]
    public async Task FleetGet_ThrowsFleetIdNotSetException()
    {
        await GetFullySetCli()
            .Command("gsh fleet get")
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardErrorContains("Required argument missing for command: 'get'.")
            .ExecuteAsync();
    }

    [Test]
    [Category("gsh")]
    [Category("gsh fleet")]
    [Category("gsh fleet get")]
    public async Task FleetGet_SucceedsWithValidInput()
    {
        await GetFullySetCli()
            .Command($"gsh fleet get {Keys.ValidFleetId}")
            .AssertNoErrors()
            .AssertStandardOutputContains("name: Test Fleet")
            .ExecuteAsync();
    }

    [Test]
    [Category("gsh")]
    [Category("gsh fleet")]
    [Category("gsh fleet list")]
    public async Task FleetList_ThrowsNotLoggedInException()
    {
        SetConfigValue("project-id", CommonKeys.ValidProjectId);
        SetConfigValue("environment-id", CommonKeys.ValidEnvironmentId);
        SetConfigValue("environment-name", CommonKeys.ValidEnvironmentName);

        await new UgsCliTestCase()
            .Command("gsh fleet list")
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardErrorContains(k_NotLoggedIn)
            .ExecuteAsync();
    }

    [Test]
    [Category("gsh")]
    [Category("gsh fleet")]
    [Category("gsh fleet list")]
    public async Task FleetList_ThrowsProjectIdNotSetException()
    {
        SetConfigValue("environment-id", CommonKeys.ValidEnvironmentId);
        SetConfigValue("environment-name", CommonKeys.ValidEnvironmentName);

        await GetLoggedInCli()
            .Command("gsh fleet list")
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardErrorContains(k_ProjectIdIsNotSet)
            .ExecuteAsync();
    }

    [Test]
    [Category("gsh")]
    [Category("gsh fleet")]
    [Category("gsh fleet list")]
    public async Task FleetList_ThrowsEnvironmentNameNotSetException()
    {
        SetConfigValue("project-id", CommonKeys.ValidProjectId);

        await GetLoggedInCli()
            .Command("gsh fleet list")
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardErrorContains(k_EnvironmentNameIsNotSet)
            .ExecuteAsync();
    }

    [Test]
    [Category("gsh")]
    [Category("gsh fleet")]
    [Category("gsh fleet list")]
    public async Task FleetList_SucceedsWithValidInput()
    {
        await GetFullySetCli()
            .Command("gsh fleet list")
            .AssertNoErrors()
            .AssertStandardOutputContains("Fetching fleet list...")
            .ExecuteAsync();
    }

    [Test]
    [Category("gsh")]
    [Category("gsh fleet")]
    [Category("gsh fleet update")]
    public async Task FleetUpdate_ThrowsNotLoggedInException()
    {
        SetConfigValue("project-id", CommonKeys.ValidProjectId);
        SetConfigValue("environment-id", CommonKeys.ValidEnvironmentId);
        SetConfigValue("environment-name", CommonKeys.ValidEnvironmentName);

        await new UgsCliTestCase()
            .Command($"gsh fleet update {Keys.ValidFleetId}")
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardErrorContains(k_NotLoggedIn)
            .ExecuteAsync();
    }

    [Test]
    [Category("gsh")]
    [Category("gsh fleet")]
    [Category("gsh fleet update")]
    public async Task FleetUpdate_ThrowsProjectIdNotSetException()
    {
        SetConfigValue("environment-id", CommonKeys.ValidEnvironmentId);
        SetConfigValue("environment-name", CommonKeys.ValidEnvironmentName);

        await GetLoggedInCli()
            .Command($"gsh fleet update {Keys.ValidFleetId}")
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardErrorContains(k_ProjectIdIsNotSet)
            .ExecuteAsync();
    }

    [Test]
    [Category("gsh")]
    [Category("gsh fleet")]
    [Category("gsh fleet update")]
    public async Task FleetUpdate_ThrowsEnvironmentNameNotSetException()
    {
        SetConfigValue("project-id", CommonKeys.ValidProjectId);
        SetConfigValue("environment-id", CommonKeys.ValidEnvironmentId);

        await GetLoggedInCli()
            .Command($"gsh fleet update {Keys.ValidFleetId}")
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardErrorContains(k_EnvironmentNameIsNotSet)
            .ExecuteAsync();
    }

    [Test]
    [Category("gsh")]
    [Category("gsh fleet")]
    [Category("gsh fleet update")]
    public async Task FleetUpdate_ThrowsFleetIdNotValidException()
    {
        await GetFullySetCli()
            .Command("gsh fleet update invalid-fleet-id")
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardErrorContains("Fleet 'invalid-fleet-id' not a valid UUID.")
            .ExecuteAsync();
    }

    [Test]
    [Category("gsh")]
    [Category("gsh fleet")]
    [Category("gsh fleet update")]
    public async Task FleetUpdate_ThrowsFleetIdNotSetException()
    {
        await GetFullySetCli()
            .Command("gsh fleet update")
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardErrorContains("Required argument missing for command: 'update'.")
            .ExecuteAsync();
    }

    [Test]
    [Category("gsh")]
    [Category("gsh fleet")]
    [Category("gsh fleet update")]
    public async Task FleetUpdate_ThrowsInvalidBuildConfigurationIdException()
    {
        await GetFullySetCli()
            .Command($"gsh fleet update {Keys.ValidFleetId} --build-configurations invalid")
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardErrorContains(
                "Cannot parse argument 'invalid' for option '--build-configurations' as expected type 'System.Int64'.")
            .ExecuteAsync();
    }

    [Test]
    [Category("gsh")]
    [Category("gsh fleet")]
    [Category("gsh fleet update")]
    public async Task FleetUpdate_SucceedsWithValidInput()
    {
        await GetFullySetCli()
            .Command($"gsh fleet update {Keys.ValidFleetId} --name updated")
            .AssertExitCode(ExitCode.Success)
            .AssertStandardOutputContains("Updating fleet...")
            .AssertStandardErrorContains("Fleet updated successfully")
            .ExecuteAsync();
    }
}
