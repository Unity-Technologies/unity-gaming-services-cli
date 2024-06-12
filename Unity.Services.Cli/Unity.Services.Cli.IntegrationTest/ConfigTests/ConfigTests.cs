using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NUnit.Framework;
using Unity.Services.Cli.Common.Exceptions;
using Unity.Services.Cli.Common.Input;
using Unity.Services.Cli.Common.Models;
using Unity.Services.Cli.Common.Validator;
using Unity.Services.Cli.IntegrationTest.Common;

namespace Unity.Services.Cli.IntegrationTest.ConfigTests;

public class ConfigTests : UgsCliFixture
{
    [SetUp]
    public void Setup()
    {
        DeleteLocalConfig();
    }

    [TearDown]
    public void TearDown()
    {
        System.Environment.SetEnvironmentVariable(Keys.EnvironmentKeys.EnvironmentName, null);
    }

    [Test]
    public async Task ConfigSetSavesToConfigFile()
    {
        await new UgsCliTestCase()
            .Command("config set environment-name test-123")
            .WaitForExit(() => AssertConfigValue("environment-name", "test-123"))
            .ExecuteAsync();
    }

    [Test]
    public async Task ConfigGetReadsFromConfigFile()
    {
        SetConfigValue("environment-name", "test-123");

        await new UgsCliTestCase()
            .Command("config get environment-name")
            .AssertNoErrors()
            .AssertStandardOutputContains("test-123")
            .ExecuteAsync();
    }

    [Test]
    public async Task ConfigGetJsonReturnsJson()
    {
        var expected = JsonConvert.SerializeObject("some-value");
        await new UgsCliTestCase()
            .Command("config set environment-name some-value")
            .Command("config get environment-name -j")
            .AssertStandardErrorContains(string.Empty)
            .AssertStandardOutputContains(expected)
            .ExecuteAsync();
    }

    [Test]
    public async Task ConfigSetEnvironmentFails()
    {
        const string expectedError = ConfigurationValidator.EnvironmentNameInvalidMessage;

        await new UgsCliTestCase()
            .Command("config set environment-name test@")
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardErrorContains(expectedError)
            .ExecuteAsync();
    }

    [Test]
    public async Task ConfigSetProjectIdFails()
    {
        await new UgsCliTestCase()
            .Command("config set project-id 1")
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardErrorContains("Your project-id is not valid. Valid input should have characters 0-9, a-f, A-F and follow the format XXXXXXXX-XXXX-XXXX-XXXX-XXXXXXXXXXXX.")
            .ExecuteAsync();
    }

    [Test]
    public async Task ConfigSetWithInvalidKeyErrorsOut()
    {
        const string expectedError = "key 'invalid-key' not allowed. Allowed values: environment-name,project-id,bucket-name";

        await new UgsCliTestCase()
            .Command("config set invalid-key random-value")
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardError(error => Assert.AreEqual(expectedError, error.Trim()))
            .ExecuteAsync();
    }

    [Test]
    public async Task ConfigGetUnsetValueReturnsError()
    {
        string expectedError = string.Format("'{0}' is not set in project configuration. '{1}' is not set in system" +
                                             " environment variables.", Keys.ConfigKeys.ProjectId, Keys.EnvironmentKeys.ProjectId);

        await new UgsCliTestCase()
            .Command("config get project-id")
            .AssertStandardError(output => StringAssert.Contains(expectedError, output))
            .AssertExitCode(ExitCode.HandledError)
            .ExecuteAsync();
    }

    [Test]
    public async Task ConfigGetUnsetConfigValueFetchesFromEnvironment()
    {
        System.Environment.SetEnvironmentVariable(Keys.EnvironmentKeys.EnvironmentName, "test-value");
        string expected = "test-value";

        await new UgsCliTestCase()
            .Command("config get environment-name")
            .AssertNoErrors()
            .AssertStandardOutput(output => StringAssert.Contains(expected, output))
            .ExecuteAsync();
    }

    void AssertConfigValue(string key, string? value)
    {
        var content = File.ReadAllText(ConfigurationFile);
        var json = JsonConvert.DeserializeObject<Dictionary<string, string>>(content);
        CollectionAssert.Contains(json, new KeyValuePair<string, string?>(key, value));
    }

    [Test]
    public async Task ConfigDeleteSpecificKeySavesToConfigFile()
    {
        const string expectedError = "Specified keys were deleted from local configuration.";
        SetConfigValue(Keys.ConfigKeys.EnvironmentName, "test-123");
        SetConfigValue(Keys.ConfigKeys.ProjectId, "00000000-0000-0000-0000-000000000000");

        await new UgsCliTestCase()
            .Command($"config delete -k {Keys.ConfigKeys.EnvironmentName} {Keys.ConfigKeys.ProjectId} -f")
            .AssertStandardErrorContains(expectedError)
            .WaitForExit(() =>
            {
                AssertConfigValue(Keys.ConfigKeys.EnvironmentName, null);
                AssertConfigValue(Keys.ConfigKeys.ProjectId, null);
            })
            .ExecuteAsync();
    }

    [Test]
    public async Task ConfigDeleteAllKeysSavesToConfigFile()
    {
        const string expectedError = "All keys were deleted from local configuration.";
        SetConfigValue(Keys.ConfigKeys.EnvironmentName, "test-123");

        await new UgsCliTestCase()
            .Command("config delete -a -f")
            .AssertStandardErrorContains(expectedError)
            .WaitForExit(() => AssertConfigValue(Keys.ConfigKeys.EnvironmentName, null))
            .ExecuteAsync();
    }

    [Test]
    public async Task ConfigDeleteWithInvalidKeyErrorsOut()
    {
        const string expectedError = "Your invalid-key is not valid. Invalid key";

        await new UgsCliTestCase()
            .Command("config delete -k invalid-key -f")
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardErrorContains(expectedError)
            .ExecuteAsync();
    }

    [Test]
    public async Task ConfigDeleteSpecifyingAllKeysWithSpecificKeysErrorsOut()
    {
        const string expectedError = $"Having both {ConfigurationInput.KeysLongAlias} and " +
                                     $"{ConfigurationInput.TargetAllKeysLongAlias} options simultaneously is unsupported.";

        await new UgsCliTestCase()
            .Command($"config delete -k {Keys.ConfigKeys.EnvironmentName} -a -f")
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardErrorContains(expectedError)
            .ExecuteAsync();
    }

    [Test]
    public async Task ConfigDeleteNoValidOptionErrorsOut()
    {
        const string expectedError = "Specify configuration keys to delete by using the " +
                                     $"{ConfigurationInput.KeysLongAlias} option. To delete all keys, use the " +
                                     $"{ConfigurationInput.TargetAllKeysLongAlias} option.";

        await new UgsCliTestCase()
            .Command("config delete")
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardErrorContains(expectedError)
            .ExecuteAsync();
    }

    [Test]
    public async Task ConfigDeleteNoForceOptionErrorsOut()
    {
        const string expectedError = "This is a destructive operation, use the --force option to continue.";

        await new UgsCliTestCase()
            .Command($"config delete -k {Keys.ConfigKeys.EnvironmentName}")
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardErrorContains(expectedError)
            .ExecuteAsync();
    }
}
