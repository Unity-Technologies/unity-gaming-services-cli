using System.CommandLine;
using Unity.Services.Cli.GameServerHosting.Input;

namespace Unity.Services.Cli.GameServerHosting.UnitTest.Input;

public class FileDownloadInputTests
{
    [TestCase(
        new[]
        {
            FileDownloadInput.OutputKey,
            "dir"
        },
        true)]
    [TestCase(
        new[]
        {
            FileDownloadInput.OutputKey,
            ""
        },
        false)]
    [TestCase(
        new[]
        {
            FileDownloadInput.OutputKey
        },
        false)]
    [TestCase(
        new string[]
            { },
        false)]
    [TestCase(null, false)]
    public void Validate_WithValidOutputInput_ReturnsTrue(string[] output, bool validates)
    {
        Assert.That(FileDownloadInput.OutputOption.Parse(output).Errors, validates ? Is.Empty : Is.Not.Empty);
    }

    [TestCase(
        new[]
        {
            FileDownloadInput.PathKey,
            "error.log"
        },
        true)]
    [TestCase(
        new[]
        {
            FileDownloadInput.PathKey,
            ""
        },
        false)]
    [TestCase(
        new[]
        {
            FileDownloadInput.PathKey
        },
        false)]
    [TestCase(
        new string[]
            { },
        false)]
    [TestCase(null, false)]
    public void Validate_WithValidPathInput_ReturnsTrue(string[] path, bool validates)
    {
        Assert.That(FileDownloadInput.PathOption.Parse(path).Errors, validates ? Is.Empty : Is.Not.Empty);
    }

    [TestCase(
        new[]
        {
            FileDownloadInput.ServerIdKey,
            "666"
        },
        true)]
    [TestCase(
        new[]
        {
            FileDownloadInput.ServerIdKey,
            "nan"
        },
        false)]
    [TestCase(
        new[]
        {
            FileDownloadInput.ServerIdKey,
            ""
        },
        false)]
    [TestCase(
        new[]
        {
            FileDownloadInput.ServerIdKey
        },
        false)]
    [TestCase(
        new string[]
            { },
        false)]
    [TestCase(null, false)]
    public void Validate_WithValidServerIdInput_ReturnsTrue(string[] serverId, bool validates)
    {
        Assert.That(FileDownloadInput.ServerIdOption.Parse(serverId).Errors, validates ? Is.Empty : Is.Not.Empty);
    }
}
