using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Unity.Services.Cli.Common.Exceptions;

namespace Unity.Services.Cli.IntegrationTest;

/// <summary>
/// A fluent API class to enable integration tests
/// </summary>
public class UgsCliTestCase
{
    IDictionary<string, string>? m_EnvironmentVariables;
    Process? m_LastProcess;
    readonly IDictionary<Process, bool> m_ProcessStartState = new Dictionary<Process, bool>();
    readonly List<Func<CancellationToken, Task>> m_Tasks = new();
    const string k_CliName = "ugs ";

    /// <summary>
    /// Adds a command to be executed to the queue.
    /// </summary>
    /// <param name="arguments">the arguments passed to the `ugs` cli</param>
    /// <returns>Instance of the test case</returns>
    /// <remarks>
    /// Note that the command name (`ugs`) is omitted. Only pass the arguments.
    /// E.g. use `config get environment` instead of `ugs config get environment`
    /// </remarks>
    public UgsCliTestCase Command(string arguments)
    {
        if (arguments.StartsWith(k_CliName))
        {
            throw new ArgumentException($"{nameof(arguments)} should not start with `ugs`.\n" +
                "only pass the arguments, without the command name `ugs`", nameof(arguments));
        }
        m_Tasks.Add(async cancellationToken =>
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = UgsCliBuilder.CliPath,
                    Arguments = arguments,
                    WorkingDirectory = UgsCliBuilder.RootDirectory,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    RedirectStandardInput = true,
                    UseShellExecute = false,
                },
                EnableRaisingEvents = true,
            };
            m_ProcessStartState[process] = false;
            if (m_LastProcess != null)
            {
                EnsureProcessStarted();
                await m_LastProcess!.WaitForExitAsync(cancellationToken);
            }
            m_LastProcess = process;
        });
        return this;
    }

    /// <summary>
    /// Set environment variables to be used when running commands
    /// </summary>
    /// <param name="environmentVariables">A dictionary of strings representing environment variables and their values</param>
    /// <returns>Instance of the test case</returns>
    public UgsCliTestCase WithEnvironmentVariables(IDictionary<string, string> environmentVariables)
    {
        m_Tasks.Add(_ =>
        {
            m_EnvironmentVariables = environmentVariables;
            return Task.CompletedTask;
        });
        return this;
    }

    /// <summary>
    /// Write to the Standard Input of the last command
    /// </summary>
    /// <param name="input">input</param>
    /// <returns>Instance of the test case</returns>
    public UgsCliTestCase StandardInputWriteLine(string input)
    {
        m_Tasks.Add(async cancellationToken =>
        {
            EnsureProcessStarted();
            await m_LastProcess!.StandardInput.WriteLineAsync(new StringBuilder(input), cancellationToken);
        });
        return this;
    }

    /// <summary>
    /// Write to the Standard Input of the last command
    /// </summary>
    /// <param name="input">input</param>
    /// <returns>Instance of the test case</returns>
    public UgsCliTestCase StandardInputWrite(string input)
    {
        m_Tasks.Add(async cancellationToken =>
        {
            EnsureProcessStarted();
            await m_LastProcess!.StandardInput.WriteAsync(new StringBuilder(input), cancellationToken);
        });
        return this;
    }

    /// <summary>
    /// Asserts that the exit code of the last command matches expected exit code
    /// </summary>
    /// <param name="exitCode">Expected exit code</param>
    /// <returns>Instance of the test case</returns>
    public UgsCliTestCase AssertExitCode(int exitCode)
    {
        m_Tasks.Add(async cancellationToken =>
        {
            EnsureProcessStarted();
            await m_LastProcess!.WaitForExitAsync(cancellationToken);
            if (exitCode != m_LastProcess.ExitCode)
            {
                var output = await m_LastProcess.StandardOutput.ReadToEndAsync();
                throw new AssertionException(
                    $"{k_CliName}{m_LastProcess.StartInfo.Arguments}{Environment.NewLine}{output}{Environment.NewLine}Expected Exit Code: {exitCode}{Environment.NewLine}But was: {m_LastProcess.ExitCode}");
            }
        });
        return this;
    }

    /// <summary>
    /// Asserts that the Standard Output of the last command matches some assert conditions
    /// </summary>
    /// <param name="outputHandler">A callback that will receive the output of the command</param>
    /// <returns>Instance of the test case</returns>
    /// <remarks>This method will not actually execute the assertion, it is up to the caller to assert.</remarks>
    public UgsCliTestCase AssertStandardOutput(Action<string> outputHandler)
    {
        m_Tasks.Add(async cancellationToken =>
        {
            EnsureProcessStarted();
            await m_LastProcess!.WaitForExitAsync(cancellationToken);
            var output = await m_LastProcess.StandardOutput.ReadToEndAsync();
            try
            {
                outputHandler(output);
            }
            catch (AssertionException)
            {
                TestContext.Write($"{k_CliName}{m_LastProcess.StartInfo.Arguments}{Environment.NewLine}{output}");
                throw;
            }
        });
        return this;
    }

    /// <summary>
    /// Asserts that the Standard Output of the last command contains expected output
    /// </summary>
    /// <param name="expectedOutput">expected command output</param>
    /// <returns></returns>
    public UgsCliTestCase AssertStandardOutputContains(string expectedOutput)
    {
        m_Tasks.Add(async cancellationToken =>
        {
            EnsureProcessStarted();
            await m_LastProcess!.WaitForExitAsync(cancellationToken);
            var output = await m_LastProcess.StandardOutput.ReadToEndAsync();
            StringAssert.Contains(expectedOutput, output);
        });
        return this;
    }

    /// <summary>
    /// Asserts that the Standard Error of the last command matches expected error
    /// </summary>
    /// <param name="outputHandler">A callback that will receive the error of the command</param>
    /// <returns>Instance of the test case</returns>
    /// <remarks>This method will not actually execute the assertion, it is up to the caller to assert.</remarks>
    public UgsCliTestCase AssertStandardError(Action<string> outputHandler)
    {
        m_Tasks.Add(async cancellationToken =>
        {
            EnsureProcessStarted();
            await m_LastProcess!.WaitForExitAsync(cancellationToken);
            var error = await m_LastProcess.StandardError.ReadToEndAsync();
            try
            {
                outputHandler(error);
            }
            catch (AssertionException)
            {
                TestContext.Write($"{k_CliName}{m_LastProcess.StartInfo.Arguments}{Environment.NewLine}{error}");
                throw;
            }
        });
        return this;
    }

    /// <summary>
    /// Asserts that the Standard error of the last command contains expected output
    /// </summary>
    /// <param name="expectedError">expected command error message</param>
    /// <returns></returns>
    public UgsCliTestCase AssertStandardErrorContains(string expectedError)
    {
        m_Tasks.Add(async cancellationToken =>
        {
            EnsureProcessStarted();
            await m_LastProcess!.WaitForExitAsync(cancellationToken);
            var output = await m_LastProcess.StandardError.ReadToEndAsync();
            StringAssert.Contains(expectedError, output);
        });
        return this;
    }

    /// <summary>
    /// Asserts that the exit code is 0, and that the Standard Error is empty
    /// </summary>
    /// <returns>Instance of the test case</returns>
    public UgsCliTestCase AssertNoErrors()
    {
        AssertExitCode(ExitCode.Success);
        AssertStandardError(Assert.IsEmpty);
        return this;
    }

    /// <summary>
    /// Waits for the last command to exit
    /// </summary>
    /// <param name="callback">A callback that runs when the process exits</param>
    /// <returns>Instance of the test case</returns>
    public UgsCliTestCase WaitForExit(Action callback)
    {
        m_Tasks.Add(async cancellationToken =>
        {
            EnsureProcessStarted();
            await m_LastProcess!.WaitForExitAsync(cancellationToken);
            callback();
        });
        return this;
    }

    /// <summary>
    /// Executes the test case, and waits for the last command to finish before returning.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token to cancel the execution of the test case</param>
    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        foreach (var task in m_Tasks)
        {
            await task(cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
        }

        if (m_LastProcess != null && m_ProcessStartState[m_LastProcess] && !m_LastProcess.HasExited)
        {
            await m_LastProcess!.WaitForExitAsync(cancellationToken);
        }
    }

    void EnsureProcessStarted()
    {
        if (m_LastProcess == null)
        {
            throw new InvalidOperationException("Process not set properly");
        }

        if (!m_ProcessStartState[m_LastProcess])
        {
            if (m_EnvironmentVariables != null)
            {
                foreach (var (key, value) in m_EnvironmentVariables)
                {
                    m_LastProcess.StartInfo.EnvironmentVariables[key] = value;
                }
            }
            m_ProcessStartState[m_LastProcess] = m_LastProcess.Start();
        }
    }
}
