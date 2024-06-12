using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using Unity.Services.Cli.CloudCode.Input;
using Unity.Services.Cli.CloudCode.Parameters;
using Unity.Services.Cli.CloudCode.Service;
using Unity.Services.Cli.Common.Exceptions;

namespace Unity.Services.Cli.CloudCode.UnitTest.Service;

[TestFixture]
class CloudCodeInputParserTests
{
    const string k_TempDirectory = "tempDirectory";
    const string k_ValidFilepath = @".\createhandlertemp.js";
    const string k_ExpectedCode = "Dummy text";

    static readonly Mock<ICloudCodeScriptParser> k_MockCloudCodeScriptParser = new();

    readonly CloudCodeInputParser m_CloudCodeInputParser = new(k_MockCloudCodeScriptParser.Object);

    [SetUp]
    public void SetUp()
    {
        File.WriteAllText(k_ValidFilepath, k_ExpectedCode);
        Directory.CreateDirectory(k_TempDirectory);
        k_MockCloudCodeScriptParser.Reset();
    }

    [TearDown]
    public void TearDown()
    {
        File.Delete(k_ValidFilepath);
        Directory.Delete(k_TempDirectory);
        k_MockCloudCodeScriptParser.Reset();
    }

    [Test]
    public void GetCloudCodeInputParserSucceed()
    {
        Assert.AreSame(k_MockCloudCodeScriptParser.Object, m_CloudCodeInputParser.CloudCodeScriptParser);
    }

    [Test]
    public void ParseLanguageSucceed()
    {
        const string expectedLanguage = "JS";
        var input = new CloudCodeInput
        {
            ScriptLanguage = expectedLanguage.ToString()
        };
        var resultLanguage = m_CloudCodeInputParser.ParseLanguage(input);
        Assert.AreEqual(expectedLanguage, resultLanguage);
    }

    [TestCase(null)]
    [TestCase("")]
    public void ParseLanguageNullOrEmptySucceed(string language)
    {
        const string expectedLanguage = "JS";
        var input = new CloudCodeInput
        {
            ScriptLanguage = language,
        };
        var resultLanguage = m_CloudCodeInputParser.ParseLanguage(input);
        Assert.AreEqual(expectedLanguage, resultLanguage);
    }

    [Test]
    public void ParseScriptTypeSucceed()
    {
        const string expected = "API";
        var input = new CloudCodeInput
        {
            ScriptType = expected.ToString()
        };
        var result = m_CloudCodeInputParser.ParseScriptType(input);
        Assert.AreEqual(expected, result);
    }

    [TestCase(null)]
    [TestCase("")]
    public void ParseScriptTypeNullOrEmptySucceed(string type)
    {
        const string expected = "API";
        var input = new CloudCodeInput
        {
            ScriptType = type
        };
        var result = m_CloudCodeInputParser.ParseScriptType(input);
        Assert.AreEqual(expected, result);
    }

    [Test]
    public async Task LoadScriptCodeSucceed()
    {
        var input = new CloudCodeInput
        {
            FilePath = k_ValidFilepath
        };
        var resultCode = await m_CloudCodeInputParser.LoadScriptCodeAsync(input, CancellationToken.None);
        Assert.AreEqual(k_ExpectedCode, resultCode);
    }

    [TestCase(null)]
    [TestCase("")]
    public void LoadScriptCodeNullOrEmptyFileNameFail(string fileName)
    {
        var input = new CloudCodeInput
        {
            FilePath = fileName
        };
        Assert.ThrowsAsync<CliException>(() => m_CloudCodeInputParser.LoadScriptCodeAsync(input, CancellationToken.None));
    }

    [Test]
    public void LoadScriptCodeUnauthorizedAccessFailed()
    {
        Directory.CreateDirectory(k_TempDirectory);
        var input = new CloudCodeInput
        {
            FilePath = k_TempDirectory
        };
        Assert.ThrowsAsync<CliException>(() => m_CloudCodeInputParser.LoadScriptCodeAsync(input, CancellationToken.None));
    }

    [Test]
    public void LoadScriptCodeFileNotFoundFailed()
    {
        var input = new CloudCodeInput
        {
            FilePath = "This should not exist"
        };
        Assert.ThrowsAsync<CliException>(() => m_CloudCodeInputParser.LoadScriptCodeAsync(input, CancellationToken.None));
    }

    [Test]
    public void LoadScriptCodeFileNotFoundWithCorrectPathFailed()
    {
        Directory.CreateDirectory(k_TempDirectory);
        var input = new CloudCodeInput
        {
            FilePath = k_TempDirectory + '/'
        };
        Assert.ThrowsAsync<CliException>(() => m_CloudCodeInputParser.LoadScriptCodeAsync(input, CancellationToken.None));
    }
}
