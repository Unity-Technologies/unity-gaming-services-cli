// =========================================================================
// Source: https://github.com/madelson/MedallionShell/blob/master/MedallionShell/Signals/WindowsProcessSignaler.cs
// License: MIT
// Last Updated: 2024-12-05
// =========================================================================

using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Unity.Services.ShellCommand.Signals;

public static class WindowsProcessSignaler
{
    /// <summary>
    /// Since signaling from the current process requires mucking with global state (CTRL handlers), we limit to one
    /// concurrent access.
    /// </summary>
    static readonly SemaphoreSlim k_SignalFromCurrentProcessLock = new SemaphoreSlim(initialCount: 1, maxCount: 1);

    /// <summary>
    /// Attempts to signal a given Windows process with the given signal. This only works if the process is in the same
    /// console group as the current process.
    /// </summary>
    /// <param name="processId">PID to send the signal to</param>
    /// <param name="signal">Signal to send</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns></returns>
    public static async Task<bool> TrySignalAsync(
        int processId,
        NativeMethods.CtrlType signal,
        CancellationToken cancellationToken = default
    )
    {
        if (HasSameConsole(processId, cancellationToken))
        {
            return await SendSignalForSameConsoleGroup(processId, signal, cancellationToken);
        }

        // We do not handle the case where a process is out of the console group.
        return false;
    }

    /// <summary>
    /// HasSameConsole determines if the process we are attempting the send the signal to shares the same console as the
    /// current process.
    /// </summary>
    /// <param name="processId">ProcessID we are attempting to send the signal to</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if the process shares the same console</returns>
    static bool HasSameConsole(
        int processId,
        CancellationToken cancellationToken
    )
    {
        uint processListCount = 1;
        uint[] processIdListBuffer;
        do
        {
            processIdListBuffer = new uint[processListCount];
            processListCount = NativeMethods.GetConsoleProcessList(processIdListBuffer, processListCount);
        }
        while (processListCount > processIdListBuffer.Length && !cancellationToken.IsCancellationRequested);

        cancellationToken.ThrowIfCancellationRequested();

        checked
        {
            return processIdListBuffer.Take((int)processListCount)
                .Contains(checked((uint)processId));
        }
    }

    static async Task<bool> SendSignalForSameConsoleGroup(
        int processId,
        NativeMethods.CtrlType signal,
        CancellationToken cancellationToken
    )
    {
        await k_SignalFromCurrentProcessLock.WaitAsync(cancellationToken);

        NativeMethods.ConsoleCtrlDelegate? signalHandler = null;
        var signalHandlerAttached = false;

        try
        {
            // We attach a signal handler for the current process to determine if we should bubble the signal or not.
            // This is due to Windows propagating signals to all processes in the same console group.
            var signalCompletionSource = new TaskCompletionSource();
            signalHandler = receivedSignal =>
            {
                if (receivedSignal != signal) return false;

                signalCompletionSource.SetResult();
                // if we're signaling another process on the same console, we return true
                // to prevent the signal from bubbling. If we're signaling ourselves, we
                // allow it to bubble since presumably that's what the caller wanted
                return processId != Environment.ProcessId;
            };

            signalHandlerAttached = NativeMethods.SetConsoleCtrlHandler(signalHandler, add: true);
            if (!signalHandlerAttached)
            {
                Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
            }


            var success = NativeMethods.GenerateConsoleCtrlEvent(
                signal,
                NativeMethods.AllProcessesWithCurrentConsoleGroup);
            if (!success)
            {
                return false;
            }

            // Wait until the signal has reached our handler and been handled to know that it is safe to
            // remove the handler. Timeout here just to ensure we don't hang forever if something weird happens
            // (e.g. someone else registers a handler concurrently with us).
            using var cancelToken = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cancelToken.CancelAfter(TimeSpan.FromSeconds(30));
            await signalCompletionSource.Task.WaitAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            // Even if we have not received the signal back, we should still consider this a success.
            return true;
        }
        finally
        {
            if (signalHandler != null && signalHandlerAttached)
            {
                var success = NativeMethods.SetConsoleCtrlHandler(signalHandler, add: false);
                if (!success)
                {
                    Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
                }
            }

            k_SignalFromCurrentProcessLock.Release();
        }

        return true;
    }
}
