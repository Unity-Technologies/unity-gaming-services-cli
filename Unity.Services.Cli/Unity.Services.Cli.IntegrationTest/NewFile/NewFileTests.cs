using System;
using System.IO;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Unity.Services.Cli.IntegrationTest.NewFile;

public class NewFileTests : UgsCliFixture
{
    const string k_NewFileBaseName = "new_file";
    const string k_RemoteConfigFileExtension = ".rc";
    const string k_CloudCodeFileExtension = ".js";
    const string k_EconomyFileExtension = ".ec";

    [TearDown]
    public void TearDown()
    {
        if (File.Exists($"{k_NewFileBaseName}{k_RemoteConfigFileExtension}"))
        {
            File.Delete($"{k_NewFileBaseName}{k_RemoteConfigFileExtension}");
        }

        if (File.Exists($"{k_NewFileBaseName}{k_CloudCodeFileExtension}"))
        {
            File.Delete($"{k_NewFileBaseName}{k_CloudCodeFileExtension}");
        }

        if (File.Exists($"{k_NewFileBaseName}{k_EconomyFileExtension}"))
        {
            File.Delete($"{k_NewFileBaseName}{k_EconomyFileExtension}");
        }
    }

    [TestCase("remote-config", k_RemoteConfigFileExtension)]
    [TestCase("cloud-code", k_CloudCodeFileExtension)]
    public async Task NewFileCreatedWithNoErrorsAndCorrectOutput(string serviceAlias, string serviceExtension)
    {
        var newFileOutPutString = $"[Information]: {Environment.NewLine}    Config file {k_NewFileBaseName}{serviceExtension} created successfully!{Environment.NewLine}";

        await new UgsCliTestCase()
            .Command($"{serviceAlias} new-file")
            .AssertStandardOutputContains(newFileOutPutString)
            .AssertNoErrors()
            .ExecuteAsync();
    }
}
