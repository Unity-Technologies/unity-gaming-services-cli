using System;
using System.IO;
using System.Threading.Tasks;
using NUnit.Framework;
using Unity.Services.Cli.IntegrationTest.Common;

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
        if (File.Exists(Path.Combine(UgsCliBuilder.RootDirectory, $"{k_NewFileBaseName}{k_RemoteConfigFileExtension}")))
        {
            File.Delete(Path.Combine(UgsCliBuilder.RootDirectory, $"{k_NewFileBaseName}{k_RemoteConfigFileExtension}"));
        }

        if (File.Exists(Path.Combine(UgsCliBuilder.RootDirectory, $"{k_NewFileBaseName}{k_CloudCodeFileExtension}")))
        {
            File.Delete(Path.Combine(UgsCliBuilder.RootDirectory, $"{k_NewFileBaseName}{k_CloudCodeFileExtension}"));
        }

        if (File.Exists(Path.Combine(UgsCliBuilder.RootDirectory, $"{k_NewFileBaseName}{k_EconomyFileExtension}")))
        {
            File.Delete(Path.Combine(UgsCliBuilder.RootDirectory, $"{k_NewFileBaseName}{k_EconomyFileExtension}"));
        }
    }

    [TestCase("remote-config", k_RemoteConfigFileExtension)]
    [TestCase("cloud-code scripts", k_CloudCodeFileExtension)]
    public async Task NewFileCreatedWithNoErrorsAndCorrectOutput(string fullParentCommand, string serviceExtension)
    {
        var newFileOutPutString = $"[Information]: {Environment.NewLine}    Config file {k_NewFileBaseName}{serviceExtension} created successfully!{Environment.NewLine}";

        await new UgsCliTestCase()
            .Command($"{fullParentCommand} new-file")
            .AssertStandardErrorContains(newFileOutPutString)
            .ExecuteAsync();
    }
}
