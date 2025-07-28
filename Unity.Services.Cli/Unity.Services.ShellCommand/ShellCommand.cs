using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using Unity.Services.ShellCommand.Signals;

namespace Unity.Services.ShellCommand;

public sealed class ShellCommand: IDisposable
{
    readonly Process m_AttachedProcess;
    readonly bool m_IsProcessOwner;
    readonly CancellationTokenSource m_CancelSource = new();

    /// <summary>
    /// Event raised when the process exits.
    /// </summary>
    public event EventHandler<ProcessExitEventArgs>? ProcessExited;

    /// <summary>
    /// PID of the process.
    /// </summary>
    public int Pid => m_AttachedProcess.Id;

    /// <summary>
    /// Exit code of the process. If the process has not exited, returns null.
    /// </summary>
    public int? ExitCode
    {
        get
        {
            if (m_AttachedProcess.HasExited)
            {
                return m_AttachedProcess.ExitCode;
            }

            return null;
        }
    }

    /// <summary>
    /// Logs produced by the process.
    /// </summary>
    public StringBuilder ProcessLogs { get; } = new();

    /// <summary>
    /// Errors produced by the process.
    /// </summary>
    public StringBuilder ProcessErrors { get; } = new();

    ShellCommand(Process attachedProcess, bool isOwner = false)
    {
        m_AttachedProcess = attachedProcess;
        m_IsProcessOwner = isOwner;

        if (!isOwner)
        {
            AcquireHandle();
        }

        AttachEventHandlers();
    }

    /// <summary>
    /// Attaches to an already running process. If the process is not found, throws an exception.
    /// </summary>
    /// <param name="pid">Process ID to attach to.</param>
    /// <returns>An instance of ShellCommand.</returns>
    /// <exception cref="ProcessNotFoundException">Thrown if no processes with specified PID is found</exception>
    public static ShellCommand AttachProcess(int pid)
    {
        Process process;
        try
        {
            process = Process.GetProcessById(pid);
        }
        catch (ArgumentException)
        {
            throw new ProcessNotFoundException(pid);
        }

        var command = new ShellCommand(process);
        return command;
    }

    /// <summary>
    /// Attaches to an already running process. If the process is not found, throws an exception.
    /// </summary>
    /// <param name="name">Name of the process to attach to. If more than one match is found, the first instance is selected.</param>
    /// <returns>An instance of ShellCommand.</returns>
    /// <exception cref="ProcessNotFoundException">Thrown if no processes with specified name is found</exception>
    public static ShellCommand AttachProcess(string name)
    {
        var processes = Process.GetProcessesByName(name);
        if (processes.Length == 0)
        {
            throw new ProcessNotFoundException(name);
        }

        var command = new ShellCommand(processes[0]);
        return command;
    }

    /// <summary>
    /// Starts a new process with the specified information.
    /// </summary>
    /// <param name="startInfo"></param>
    /// <exception cref="ArgumentException">Thrown if provided start information is invalid.</exception>
    /// <returns></returns>
    public static ShellCommand StartCommand(StartCommandInfo startInfo)
    {
        if (startInfo.Program == string.Empty)
        {
            throw new ArgumentException("Program name cannot be empty", nameof(startInfo));
        }

        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = startInfo.Program,
                Arguments = string.Join(" ", startInfo.Arguments),
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                RedirectStandardInput = true,
                UseShellExecute = false,
                CreateNoWindow = false, // This needs to be set to 'false', so the process gets attached to the same Console Group on Windows.
                WorkingDirectory = startInfo.Directory,
                StandardErrorEncoding = Encoding.UTF8,
                StandardInputEncoding = Encoding.UTF8,
                StandardOutputEncoding = Encoding.UTF8,
            },
        };

        foreach (var envVar in startInfo.EnvironmentVariables)
        {
            process.StartInfo.EnvironmentVariables.Add(envVar.Key, envVar.Value);
        }

        var command = new ShellCommand(process, true);

        process.Start();
        command.StartReadingOutputs();

        return command;
    }

    void AcquireHandle()
    {
        var _ = m_AttachedProcess.SafeHandle;
    }

    void AttachEventHandlers()
    {
        m_AttachedProcess.EnableRaisingEvents = true;
        m_AttachedProcess.Exited += HandleProcessExited;
    }

    void StartReadingOutputs()
    {
        Task.Run(() => ReadOutputAsync(m_AttachedProcess.StandardOutput, ProcessLogs, m_CancelSource.Token), CancellationToken.None);
        Task.Run(() => ReadOutputAsync(m_AttachedProcess.StandardError, ProcessErrors, m_CancelSource.Token), CancellationToken.None);
    }

    /// <summary>
    /// Stops the process gracefully. If the process is not stopped before the cancellation token requests a cancellation,
    /// it will be killed.
    /// </summary>
    /// <param name="cancellationToken">CancellationToken.</param>
    /// <exception cref="InvalidOperationException"></exception>
    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (m_AttachedProcess is { HasExited: true }) return;

        try
        {
            if (OperatingSystem.IsWindows())
            {
                await CloseWindowsApplicationAsync(cancellationToken);
            }
            else
            {
                SendUnixInterruptSignal(cancellationToken);
            }

            await WaitForExitAsync(cancellationToken);
            await m_CancelSource.CancelAsync().WaitAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            if (m_AttachedProcess is { HasExited: false })
            {
                m_AttachedProcess.Kill(true);
            }

            if (ex is not OperationCanceledException)
            {
                throw new InvalidOperationException("Failed to gracefully stop the process", ex);
            }
        }
    }

    public void Dispose()
    {
        if (m_AttachedProcess is { HasExited: false } && m_IsProcessOwner)
        {
            m_AttachedProcess.Kill(true);
        }

        m_AttachedProcess.EnableRaisingEvents = false;
        m_AttachedProcess.Exited -= HandleProcessExited;

        ProcessExited = null;

        m_AttachedProcess.Dispose();

        if (!m_CancelSource.IsCancellationRequested)
        {
            m_CancelSource.Cancel();
        }
        m_CancelSource.Dispose();
    }

    /// <summary>
    /// Sends a UNIX interrupt signal to the process. Works only on POSIX systems.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if for some reason, we fail to send the interrupt signal</exception>
    /// <exception cref="PlatformNotSupportedException">Thrown if the OS is not POSIX compliant</exception>
    void SendUnixInterruptSignal(CancellationToken cancellationToken)
    {
        if (!OperatingSystem.IsLinux() && !OperatingSystem.IsMacOS() && !OperatingSystem.IsFreeBSD())
        {
            throw new PlatformNotSupportedException("Cannot use this method on Windows");
        }

        cancellationToken.ThrowIfCancellationRequested();

        var success = PosixProcessSignaler.TrySignal(m_AttachedProcess.Id, ((int)PosixSignal.SIGINT)*-1);
        if (!success)
        {
            throw new InvalidOperationException("Failed to send the interrupt signal to the process");
        }
    }

    /// <summary>
    /// This attempts to close the process gracefully. It first tries to close the main window of the process. If it cannot,
    /// it sends a Ctrl+C signal to the process. If it can't (because the process got detached from the Console Group somehow),
    /// it kills the process.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <exception cref="PlatformNotSupportedException">Thrown if used on an unsupported operating system</exception>
    async Task CloseWindowsApplicationAsync(CancellationToken cancellationToken)
    {
        if (!OperatingSystem.IsWindows())
        {
            throw new PlatformNotSupportedException("Cannot use this method on non-Windows platforms");
        }

        cancellationToken.ThrowIfCancellationRequested();

        var success = m_AttachedProcess.CloseMainWindow();
        if (!success)
        {
            // The success being false here is likely due to the process being a Console application rather than
            // a windowed one. In this case, we attempt to send a Ctrl+C signal to the process. This only works if
            // the application is deployed in the same console group.
            success = await WindowsProcessSignaler.TrySignalAsync(
                m_AttachedProcess.Id,
                NativeMethods.CtrlType.CTRL_C_EVENT,
                cancellationToken);
        }

        if (!success)
        {
            // This is a last resort. We kill the process.
            m_AttachedProcess.Kill(true);
        }
    }

    /// <summary>
    /// Waits for a Windows application with a GUI to be launched and ready.
    /// Only works on specifically Windows GUI applications. Using this on a non-Windowed application will simply be
    /// ignored and will return immediately.
    /// </summary>
    /// <param name="cancellationToken"></param>
    public async Task WaitForWindowsAppReadyAsync(CancellationToken cancellationToken = default)
    {
        if (!OperatingSystem.IsWindows())
        {
            // This simply does not work on non-Windows Processes anyway...
            return;
        }

        try
        {
            var ok = m_AttachedProcess.WaitForInputIdle(TimeSpan.FromSeconds(5));
            if (!ok)
            {
                throw new TimeoutException("Timeout waiting for process to ready");
            }
        }
        catch (InvalidOperationException)
        {
            // This could be because the process does not have a graphical interface.
            return;
        }

        cancellationToken.ThrowIfCancellationRequested();

        var mainWindowHandle = IntPtr.Zero;
        while (mainWindowHandle == IntPtr.Zero && !cancellationToken.IsCancellationRequested)
        {
            mainWindowHandle = m_AttachedProcess.MainWindowHandle;

            if (mainWindowHandle == IntPtr.Zero)
            {
                await Task.Delay(50, cancellationToken);
            }
        }
    }

    /// <summary>
    /// Waits for the attached process to exit.
    /// </summary>
    /// <param name="cancellationToken">CancellationToken</param>
    public async Task WaitForExitAsync(CancellationToken cancellationToken = default)
    {
        await m_AttachedProcess.WaitForExitAsync(cancellationToken);
    }

    /// <summary>
    /// Waits for the attached process to produce a log that matches the provided pattern.
    /// </summary>
    /// <param name="logPattern">Log pattern in the form of a regex</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task WaitForLogToBePresentAsync(Regex logPattern, CancellationToken cancellationToken = default)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            if (logPattern.IsMatch(ProcessLogs.ToString()))
            {
                return;
            }

            await Task.Delay(50, cancellationToken);
        }

        cancellationToken.ThrowIfCancellationRequested();
    }

    void HandleProcessExited(object? _, EventArgs __)
    {
        OnProcessExited(false);
    }

    void OnProcessExited(bool wasKilled)
    {
        DateTime? exitTime = null;
        int? exitCode = null;

        if (wasKilled)
        {
            exitTime = DateTime.Now;
            exitCode = -1;
        }
        else
        {
            m_AttachedProcess.Refresh();

            var t = m_AttachedProcess.ExitTime;
            exitTime = t;

            // Sometimes, this date gets corrupted for some reason. We don't need a lot of accuracy here,
            // so we will fake the process exit time.
            if (Math.Abs((t - DateTime.Now).Milliseconds) > TimeSpan.FromMinutes(5).Milliseconds)
            {
                exitTime = DateTime.Now;
            }

            exitCode = m_AttachedProcess.ExitCode;
        }

        var e = new ProcessExitEventArgs()
        {
            ProcessId = m_AttachedProcess.Id,
            ExitCode = exitCode.Value,
            ExitedAt = exitTime.Value,
        };
        ProcessExited?.Invoke(this, e);
    }

    /// <summary>
    /// Reads the output of a process stream and appends it to the specified StringBuilder until the cancellation token
    /// is canceled.
    /// </summary>
    /// <param name="reader">StreamReader of the output to read</param>
    /// <param name="destination">Destination StringBuffer</param>
    /// <param name="cancellationToken">Cancellation token</param>
    static async Task ReadOutputAsync(StreamReader reader, StringBuilder destination, CancellationToken cancellationToken)
    {
        var buffer = new char[1024];

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var length = await reader.ReadAsync(buffer, cancellationToken);
                if (length > 0)
                {
                    destination.Append(buffer.AsSpan(0, length));
                }
            }
            catch (OperationCanceledException) { }
        }
    }
}
