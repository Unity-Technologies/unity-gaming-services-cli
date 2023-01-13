using NUnit.Framework;
using Unity.Services.Cli.Common.SystemEnvironment;

namespace Unity.Services.Cli.Common.UnitTest.SystemEnvironment;

[TestFixture]
public class SystemEnvironmentUtilitiesTests
{
    SystemEnvironmentProvider? m_EnvUtilities;
    const string k_TestEnvironmentGetKey = "UGS_CLI_TEST_KEY_0000";
    const string k_TestEnvironmentSetKey = "UGS_CLI_TEST_KEY_0001";

    [SetUp]
    public void SetUp()
    {
        m_EnvUtilities = new();
    }

    [TearDown]
    public void TearDown()
    {
        System.Environment.SetEnvironmentVariable(k_TestEnvironmentSetKey, null);
    }

    [Test]
    public void GetEnvironment_ReturnsValueWhenKeyIsSet()
    {
        System.Environment.SetEnvironmentVariable(k_TestEnvironmentSetKey, "test-value");
        string? value = m_EnvUtilities!.GetSystemEnvironmentVariable(k_TestEnvironmentSetKey, out string errorMsg);
        Assert.AreEqual("", errorMsg);
    }

    [Test]
    public void GetEnvironment_ReturnsNullValueWhenKeyIsNotSet()
    {
        string? value = m_EnvUtilities!.GetSystemEnvironmentVariable(k_TestEnvironmentGetKey, out string errorMsg);
        Assert.AreEqual(null, value);
    }

    [Test]
    public void GetEnvironment_ReturnsErrorMessageWhenKeyNotSet()
    {
        const string expectedError = "UGS_CLI_TEST_KEY_0000 is not set in system environment variables.";
        m_EnvUtilities!.GetSystemEnvironmentVariable(k_TestEnvironmentGetKey, out string errorMsg);
        StringAssert.Contains(expectedError, errorMsg);
    }
}
