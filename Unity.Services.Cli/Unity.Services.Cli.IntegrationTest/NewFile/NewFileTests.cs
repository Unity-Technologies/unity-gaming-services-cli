using System;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Unity.Services.Cli.IntegrationTest.NewFile;

public class NewFileTests : UgsCliFixture
{
    [Test]
    public async Task NewFileCreatedWithNoErrorsAndCorrectOutput()
    {
        var newFileOutPutString = $"[Information]: {Environment.NewLine}    Config file new_config.rc created successfully!{Environment.NewLine}";
        await new UgsCliTestCase()
            .Command($"rc new-file")
            .AssertStandardOutputContains(newFileOutPutString)
            .AssertNoErrors()
            .ExecuteAsync();
    }
}
