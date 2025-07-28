namespace Unity.Services.ShellCommand;

public class ProcessNotFoundException: Exception
{
    public ProcessNotFoundException(int pid)
        : base($"Process with PID {pid} not found")
    {
    }

    public ProcessNotFoundException(string processName)
        : base($"Process with name {processName} not found")
    {
    }
}
