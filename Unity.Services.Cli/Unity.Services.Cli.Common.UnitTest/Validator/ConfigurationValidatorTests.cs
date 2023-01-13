using NUnit.Framework;
using Unity.Services.Cli.Common.Exceptions;
using Unity.Services.Cli.Common.Validator;

namespace Unity.Services.Cli.Common.UnitTest.Validator;

[TestFixture]
class ConfigurationValidatorTests
{
    ConfigurationValidator m_ConfigurationValidator = new();

    public static IEnumerable<TestCaseData> ValidatePreferencesTestCases
    {
        get
        {
            // environment id test cases
            yield return new TestCaseData(Models.Keys.ConfigKeys.EnvironmentId, "11111111-1111-a1b2-a1b2-a1b2c3d4e5f6",
                true, string.Empty);
            yield return new TestCaseData(Models.Keys.ConfigKeys.EnvironmentId, "invalid envId", false,
                ConfigurationValidator.GuidInvalidMessage);
            yield return new TestCaseData(Models.Keys.ConfigKeys.EnvironmentId, string.Empty, false,
                ConfigurationValidator.NullValueMsg);
            yield return new TestCaseData(Models.Keys.ConfigKeys.EnvironmentId, "invalidEnvId", false,
                ConfigurationValidator.GuidInvalidMessage);

            // environment name test cases
            yield return new TestCaseData(Models.Keys.ConfigKeys.EnvironmentName, "validenv", true, string.Empty);
            yield return new TestCaseData(Models.Keys.ConfigKeys.EnvironmentName, "a", true, string.Empty);
            yield return new TestCaseData(Models.Keys.ConfigKeys.EnvironmentName, "a-_b", true, string.Empty);
            yield return new TestCaseData(Models.Keys.ConfigKeys.EnvironmentName, "invalid env", false,
                ConfigurationValidator.EnvironmentNameInvalidMessage);
            yield return new TestCaseData(Models.Keys.ConfigKeys.EnvironmentName, "", false,
                ConfigurationValidator.NullValueMsg);

            // project id test cases
            yield return new TestCaseData(Models.Keys.ConfigKeys.ProjectId, "11111111-1111-a1b2-a1b2-a1b2c3d4e5f6",
                true, string.Empty);
            yield return new TestCaseData(Models.Keys.ConfigKeys.ProjectId, "invalid projId", false,
                ConfigurationValidator.GuidInvalidMessage);
            yield return new TestCaseData(Models.Keys.ConfigKeys.ProjectId, string.Empty, false,
                ConfigurationValidator.NullValueMsg);
            yield return new TestCaseData(Models.Keys.ConfigKeys.ProjectId, "invalidProjectId", false,
                ConfigurationValidator.GuidInvalidMessage);

            // Null or empty key and value tests
            yield return new TestCaseData(null, "123", false,
                ConfigurationValidator.NullKeyMsg);
            yield return new TestCaseData("", "123", false,
                ConfigurationValidator.NullKeyMsg);
            yield return new TestCaseData("project-id", null, false,
                ConfigurationValidator.NullValueMsg);
            yield return new TestCaseData("project-id", "", false,
                ConfigurationValidator.NullValueMsg);

            // wrong key test cases
            yield return new TestCaseData("invalid-option", "abc", false, ConfigurationValidator.InvalidKeyMsg);
        }
    }

    [TestCaseSource(nameof(ValidatePreferencesTestCases))]
    public void IsConfigValidTest(string key, string value, bool expectedResult, string expectedErrorMsg)
    {
        bool isConfigValid = m_ConfigurationValidator!.IsConfigValid(key, value, out var errorMessage);
        Assert.AreEqual(expectedResult, isConfigValid);
        Assert.AreEqual(expectedErrorMsg, errorMessage);
    }

    [TestCase(null)]
    [TestCase("")]
    public void IsKeyValidNullKeyTest(string key)
    {
        bool isKeyValid = m_ConfigurationValidator.IsKeyValid(key, out var errorMessage);
        Assert.IsFalse(isKeyValid);
        Assert.AreEqual(ConfigurationValidator.NullKeyMsg, errorMessage);
    }

    [TestCase("abc")]
    public void IsKeyValidInvalidKeyTest(string key)
    {
        bool isKeyValid = m_ConfigurationValidator.IsKeyValid(key, out var errorMessage);
        Assert.IsFalse(isKeyValid);
        Assert.AreEqual(ConfigurationValidator.InvalidKeyMsg, errorMessage);
    }

    [TestCase("project-id")]
    public void IsKeyValidValidKeyTest(string key)
    {
        bool isKeyValid = m_ConfigurationValidator.IsKeyValid(key, out var errorMessage);
        Assert.IsTrue(isKeyValid);
        Assert.AreEqual(string.Empty, errorMessage);
    }

    [TestCase("invalid-key", "invalid-value")]
    [TestCase(Models.Keys.ConfigKeys.ProjectId, "invalid-value")]
    [TestCase(Models.Keys.ConfigKeys.EnvironmentId, "invalid-value")]
    [TestCase(Models.Keys.ConfigKeys.EnvironmentName, " ")]
    public void InvalidConfigThrowTest(string key, string value)
    {
        Assert.Throws<ConfigValidationException>(() => m_ConfigurationValidator.ThrowExceptionIfConfigInvalid(key, value));
    }
}
