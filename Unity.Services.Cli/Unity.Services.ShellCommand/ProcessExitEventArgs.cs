namespace Unity.Services.ShellCommand;

public class ProcessExitEventArgs
{
    public int ProcessId { get; init; }
    public int ExitCode { get; init; }
    public DateTime ExitedAt { get; init; }
}
