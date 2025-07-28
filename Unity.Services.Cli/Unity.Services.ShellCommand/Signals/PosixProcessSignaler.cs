// =========================================================================
// Source: https://github.com/madelson/MedallionShell/blob/master/MedallionShell/Signals/UnixProcessSignaler.cs
// License: MIT
// Last Updated: 2024-12-05
// =========================================================================

namespace Unity.Services.ShellCommand.Signals;

public static class PosixProcessSignaler
{
    /// <summary>
    /// Attempts to signal a given POSIX process with the given signal.
    /// Works only in POSIX systems (Linux, macOS, etc).
    /// </summary>
    /// <param name="processId">PID to send the signal to</param>
    /// <param name="signal">Signal to send</param>
    /// <returns>True is sending the signal was successful.</returns>
    public static bool TrySignal(int processId, int signal)
    {
        return NativeMethods.kill(pid: processId, sig: signal) == 0;
    }
}
