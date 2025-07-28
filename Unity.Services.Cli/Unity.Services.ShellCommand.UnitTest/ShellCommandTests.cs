using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace Unity.Services.ShellCommand.UnitTest;

public class ShellCommandTests
{
    readonly Regex m_WaitRegex = new Regex(
        @"^Waiting for SIGINT \(Ctrl \+ C\) to exit(\r)?$",
        RegexOptions.Multiline | RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant
    );

    [Test]
    public void StartCommand_ShouldHaveProgram()
    {
        var startInfo = new StartCommandInfo
        {
            Program = string.Empty,
        };

        Assert.Throws<ArgumentException>(() => ShellCommand.StartCommand(startInfo));
    }

    [Test]
    public async Task StartCommand_ShouldStopProperly()
    {
        var cancelSource = new CancellationTokenSource();
        cancelSource.CancelAfter(10000);

        using var command = StartMockProcess(0);

        try
        {
            await command.WaitForLogToBePresentAsync(m_WaitRegex, cancelSource.Token);
        }
        catch (OperationCanceledException)
        {
            Assert.Fail($"Unable to locate the appropriate logs\r\n{command.ProcessLogs}\r\nSearch regex: {m_WaitRegex}");
        }

        await command.StopAsync(cancelSource.Token);

        // Asserting exits codes is NOTORIOUSLY flaky, so we're just going to make sure it's not null
        Assert.That(command.ExitCode, Is.Not.Null, "Exit code should be defined");
    }

    [Test]
    public async Task StartCommand_ShouldFallbackToKill()
    {
        using var cancelSource = new CancellationTokenSource();
        cancelSource.CancelAfter(10000);

        using var command = StartMockProcess(0);

        try
        {
            await command.WaitForLogToBePresentAsync(m_WaitRegex, cancelSource.Token);
        }
        catch (OperationCanceledException)
        {
            Assert.Fail($"Unable to locate the appropriate logs\r\n{command.ProcessLogs}\r\nSearch regex: {m_WaitRegex}");
        }

        var evntCount = 0;
        ProcessExitEventArgs? evnt = null;
        command.ProcessExited += (_, args) =>
        {
            evntCount++;
            evnt = args;
        };

        // This will cause the StopAsync process to fail, and fallback to Kill
        var stopCancelSource = CancellationTokenSource.CreateLinkedTokenSource(cancelSource.Token);
        await stopCancelSource.CancelAsync();
        await command.StopAsync(stopCancelSource.Token);

        await command.WaitForExitAsync(cancelSource.Token);

        while (evnt == null && !cancelSource.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(50, cancelSource.Token);
            } catch (OperationCanceledException) { }
        }

        Assert.Multiple(
            () =>
            {
                Assert.That(evnt, Is.Not.Null, "should have received an exit event");
                Assert.That(evntCount, Is.EqualTo(1), "should only have received one event");
            });
    }

    [Test]
    public async Task Dispose_ShouldKillProcess()
    {
        using var cancelSource = new CancellationTokenSource();
        cancelSource.CancelAfter(10000);

        ShellCommand? command = null;
        ShellCommand? attachedCommand = null;

        try
        {
            try
            {
                command = StartMockProcess(0);
                attachedCommand = ShellCommand.AttachProcess(command.Pid);

                try
                {
                    await command.WaitForLogToBePresentAsync(m_WaitRegex, cancelSource.Token);
                }
                catch (OperationCanceledException)
                {
                    Assert.Fail(
                        $"Unable to locate the appropriate logs\r\n{command.ProcessLogs}\r\nSearch regex: {m_WaitRegex}");
                }
            }
            finally
            {
                Assert.DoesNotThrow(() => command?.Dispose());
            }

            await attachedCommand.WaitForExitAsync(cancelSource.Token);

            Assert.That(attachedCommand.ExitCode, Is.Not.Null, "Exit code should be defined");
        }
        finally
        {
            attachedCommand?.Dispose();
        }
    }

    [Test]
    public void AttachToPid_WhenProcessIdDoesNotExist_ShouldThrow()
    {
        var cancelSource = new CancellationTokenSource();
        cancelSource.CancelAfter(10000);

        var pid = GetUnusedPid(cancelSource.Token);
        Assert.Throws<ProcessNotFoundException>(() => ShellCommand.AttachProcess(pid));
    }

    [Test]
    public void AttachToProcessName_WhenProcessNameDoesNotExist_ShouldThrow()
    {
        Assert.Throws<ProcessNotFoundException>(() => ShellCommand.AttachProcess("Unity.Services.Cli.NonExistentProcess"));
    }

    [Test]
    public void AttachToProcessName_ShouldSucceed()
    {
        // dotnet is supposed to always be running when the tests are themselves running
        Assert.DoesNotThrow(() => ShellCommand.AttachProcess("dotnet"));
    }

    [Test]
    public async Task AttachToExternalProcess_ShouldReportExitEvent()
    {
        var startTime = DateTime.Now;

        var cancelSource = new CancellationTokenSource();
        cancelSource.CancelAfter(10000);

        using var command = StartMockProcess(77);

        try
        {
            await command.WaitForLogToBePresentAsync(m_WaitRegex, cancelSource.Token);
        }
        catch (OperationCanceledException)
        {
            Assert.Fail($"Unable to locate the appropriate logs\r\n{command.ProcessLogs}\r\nSearch regex: {m_WaitRegex}");
        }

        var attachedCommand = ShellCommand.AttachProcess(command.Pid);
        var eventCount = 0;
        ProcessExitEventArgs? evnt = null;
        attachedCommand.ProcessExited += (_, e) =>
        {
            evnt = e;
            eventCount++;
        };

        await command.StopAsync(cancelSource.Token);
        await attachedCommand.WaitForExitAsync(cancelSource.Token);

        while (evnt == null && !cancelSource.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(50, cancelSource.Token);
            } catch (OperationCanceledException) { }
        }

        Assert.Multiple(
            () =>
            {
                Assert.That(evnt, Is.Not.Null, "event should have been raised");
                Assert.That(eventCount, Is.EqualTo(1), "event should have been raised exactly once");

                Assert.That(evnt?.ExitCode, Is.EqualTo(77), "expected exit code 77");

                Assert.That(evnt?.ExitedAt, Is.GreaterThan(startTime));
                Assert.That(evnt?.ExitedAt, Is.LessThan(DateTime.Now));
            });
    }

    [Test]
    public async Task AttachToExternalWindowsApp_ShouldReportExitEvent()
    {
        if (!OperatingSystem.IsWindows())
        {
            Assert.Ignore("This test is only supported on Windows");
        }

        var startTime = DateTime.Now;

        var cancelSource = new CancellationTokenSource();
        cancelSource.CancelAfter(10000);

        using var command = StartMockWindowsApp();

        var attachedCommand = ShellCommand.AttachProcess(command.Pid);
        ProcessExitEventArgs? evnt = null;
        var eventCount = 0;
        attachedCommand.ProcessExited += (_, e) =>
        {
            evnt = e;
            eventCount++;
        };

        await command.WaitForWindowsAppReadyAsync(cancelSource.Token);

        await command.StopAsync(cancelSource.Token);
        await attachedCommand.WaitForExitAsync(cancelSource.Token);

        while (evnt == null && !cancelSource.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(50, cancelSource.Token);
            } catch (OperationCanceledException) { }
        }

        Assert.Multiple(
            () =>
            {
                Assert.That(evnt, Is.Not.Null, "event should have been raised");
                Assert.That(eventCount, Is.EqualTo(1), "event should have been raised exactly once");

                Assert.That(evnt?.ExitedAt, Is.GreaterThan(startTime));
                Assert.That(evnt?.ExitedAt, Is.LessThan(DateTime.Now));
            });
    }

    static int GetUnusedPid(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var maybePid = Random.Shared.Next(1000, 65536);
            try
            {
                Process.GetProcessById(maybePid);
            }
            catch (ArgumentException)
            {
                // If the error is raised, the PID was not found
                return maybePid;
            }
        }

        cancellationToken.ThrowIfCancellationRequested();
        return -1;  // Will never happen
    }

    static ShellCommand StartMockProcess(int statusCode = 0)
    {
        // This relies on the Target 'BuildMockProcessBeforeTests' in the csproj being properly configured
        var solutionPath = GetSolutionAbsolutePath();
        var executablePath = Path.Combine(solutionPath, "Unity.Services.ShellCommand.MockCli", "bin", "Debug", "net8.0", "Unity.Services.ShellCommand.MockCli");
        if (OperatingSystem.IsWindows())
        {
            executablePath += ".exe";
        }

        var startInfo = new StartCommandInfo
        {
            Program = executablePath,
            Arguments = [statusCode.ToString()],
            EnvironmentVariables = new Dictionary<string, string>
            {
                {
                    "TEST_ENV", "TRUE"
                }
            }
        };

        return ShellCommand.StartCommand(startInfo);
    }

    static ShellCommand StartMockWindowsApp()
    {
        if (!OperatingSystem.IsWindows())
        {
            throw new PlatformNotSupportedException("This operation is only supported on Windows");
        }

        // This relies on the Target 'BuildMockProcessBeforeTests' in the csproj being properly configured
        var solutionPath = GetSolutionAbsolutePath();
        var executablePath = Path.Combine(solutionPath, "Unity.Services.ShellCommand.MockWinApp", "bin", "Debug", "net8.0-windows", "Unity.Services.ShellCommand.MockWinApp.exe");

        var startInfo = new StartCommandInfo
        {
            Program = executablePath
        };

        return ShellCommand.StartCommand(startInfo);
    }

    static string GetSolutionAbsolutePath()
    {
        var pathParts = System.Environment.CurrentDirectory.Split(Path.DirectorySeparatorChar);
        var solutionPath = new StringBuilder();

        foreach (var pathPart in pathParts)
        {
            solutionPath.Append(pathPart);
            solutionPath.Append(Path.DirectorySeparatorChar);

            var isSolutionPath = pathPart == "Unity.Services.Cli";
            if (isSolutionPath)
            {
                return solutionPath.ToString();
            }
        }

        throw new InvalidOperationException("Could not find solution path");
    }
}
