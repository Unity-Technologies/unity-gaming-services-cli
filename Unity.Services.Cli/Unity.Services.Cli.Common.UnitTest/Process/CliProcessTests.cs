using NUnit.Framework;
using Unity.Services.Cli.Common.Process;

namespace Unity.Services.Cli.Common.UnitTest.Process;

[TestFixture]
class CliProcessTests
{
    CliProcess m_CliProcess = new();

    [Test]
    public async Task ProcessExecuteDotnetVersion()
    {
        var dotnetVersion = await m_CliProcess.ExecuteAsync("dotnet", System.Environment.CurrentDirectory, new[]
        {
            "--version"
        }, CancellationToken.None);
        Assert.True(Version.TryParse(dotnetVersion, out _));
    }

    [Test]
    public void ProcessExecuteDotnetInvalidArgumentThrowException()
    {
        Assert.ThrowsAsync<ProcessException>(async () => await m_CliProcess.ExecuteAsync("dotnet",
            System.Environment.CurrentDirectory, new[]
            {
                "--foo"
            }, CancellationToken.None));
    }

    [Test]
    public void ProcessExecuteInvalidExecutableThrowException()
    {
        Assert.ThrowsAsync<ProcessException>(async () => await m_CliProcess.ExecuteAsync("foo",
            System.Environment.CurrentDirectory, new[]
            {
                "--foo"
            }, CancellationToken.None));
    }
}
