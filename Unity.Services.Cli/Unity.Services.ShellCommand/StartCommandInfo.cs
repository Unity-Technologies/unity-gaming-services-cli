namespace Unity.Services.ShellCommand;

public class StartCommandInfo
{
    /// <summary>
    /// Program to start.
    /// </summary>
    public string Program { get; init; } = string.Empty;

    /// <summary>
    /// Arguments to provide the program.
    /// </summary>
    public string[] Arguments { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Working directory for the command.
    /// </summary>
    public string Directory { get; init; } = Environment.CurrentDirectory;

    /// <summary>
    /// Environment variables to set for the command.
    /// </summary>
    public IDictionary<string, string> EnvironmentVariables { get; set; } = new Dictionary<string, string>();
}
