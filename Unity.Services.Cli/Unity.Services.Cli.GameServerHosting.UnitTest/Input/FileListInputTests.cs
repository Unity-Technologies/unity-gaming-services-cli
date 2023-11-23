using System.CommandLine;
using Unity.Services.Cli.GameServerHosting.Input;

namespace Unity.Services.Cli.GameServerHosting.UnitTest.Input;

[TestFixture]
public class FileListInputTests
{
    [TestCase("2018-07-22T18:32:25Z", true, TestName = "Golden path")]
    [TestCase("3000-02-01T12:00:00Z", false, TestName = "Invalid ModifiedFrom, provided date is in the future")]
    [TestCase("invalid", false, TestName = "Invalid date format provided for ModifiedFrom")]
    public void Validate_WithValidModifiedFromInput_ReturnsTrue(string modifiedFrom, bool validates)
    {
        var arg = new[]
        {
            FileListInput.ModifiedFromKey,
            modifiedFrom
        };
        Assert.That(FileListInput.ModifiedFromOption.Parse(arg).Errors, validates ? Is.Empty : Is.Not.Empty);
    }

    [TestCase("2018-07-22T18:32:25Z", true, TestName = "Golden path")]
    [TestCase("3000-02-01T12:00:00Z", false, TestName = "Invalid ModifiedTo, provided date is in the future")]
    [TestCase("invalid", false, TestName = "Invalid date format provided for ModifiedTo")]
    public void Validate_WithValidModifiedToInput_ReturnsTrue(string modifiedTo, bool validates)
    {
        var arg = new[]
        {
            FileListInput.ModifiedToKey,
            modifiedTo
        };
        Assert.That(FileListInput.ModifiedToOption.Parse(arg).Errors, validates ? Is.Empty : Is.Not.Empty);
    }

    [TestCase(new[] { "1", "2" }, true, TestName = "Golden path")]
    [TestCase(null, false, TestName = "Server Ids as null")]
    [TestCase(new string[] { }, false, TestName = "Server Ids as empty array")]
    public void Validate_WithValidServerIdsInput_ReturnsTrue(string[]? serverIds, bool validates)
    {
        serverIds = (serverIds ?? Array.Empty<string>()).Prepend(FileListInput.ServerIdKey).ToArray();
        Assert.That(FileListInput.ServerIdOption.Parse(serverIds).Errors, validates ? Is.Empty : Is.Not.Empty);
    }
}
