// =========================================================================
// Source: https://github.com/madelson/MedallionShell/blob/master/MedallionShell/Signals/NativeMethods.cs
// License: MIT
// Last Updated: 2024-12-05
// =========================================================================

using System.Runtime.InteropServices;

namespace Unity.Services.ShellCommand.Signals;

public static class NativeMethods
{
    // from https://docs.microsoft.com/en-us/windows/console/generateconsolectrlevent
    public const uint AllProcessesWithCurrentConsoleGroup = 0;

    // ReSharper disable InconsistentNaming
    // reason: these are matching the Windows API
    public enum CtrlType : uint
    {
        CTRL_C_EVENT = 0,
        CTRL_BREAK_EVENT = 1
    }
    // ReSharper restore InconsistentNaming

    public delegate bool ConsoleCtrlDelegate(CtrlType ctrlType);

    // PC003 complains about methods that aren't supported on UWP. I looked into multi-targeting against
    // UWP with https://github.com/dotnet/sdk/issues/1408 and https://github.com/onovotny/MSBuildSdkExtras,
    // but since UWP doesn't even support Process this didn't feel worthwhile
#pragma warning disable PC003
    /// <summary>
    /// Retrieves a list of the processes attached to the current console.
    /// </summary>
    /// <param name="lpdwProcessList">Buffer where all process ids will be dumped into</param>
    /// <param name="dwProcessCount">Total number of processes attached to the current console</param>
    /// <see>https://docs.microsoft.com/en-us/windows/console/getconsoleprocesslist</see>
    /// <returns></returns>
    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern uint GetConsoleProcessList(uint[] lpdwProcessList, uint dwProcessCount);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern bool SetConsoleCtrlHandler(ConsoleCtrlDelegate? handlerRoutine, bool add);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern bool GenerateConsoleCtrlEvent(CtrlType dwCtrlEvent, uint dwProcessGroupId);

    // http://man7.org/linux/man-pages/man2/kill.2.html
    // from https://developers.redhat.com/blog/2019/03/25/using-net-pinvoke-for-linux-system-functions/
    [DllImport("libc", SetLastError = true)]
#pragma warning disable SA1300
    public static extern int kill(int pid, int sig);
#pragma warning restore SA1300
#pragma warning restore PC003
}
