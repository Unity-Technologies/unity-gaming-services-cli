namespace Unity.Services.Cli.Common.Process;

/// <summary>
/// Process to execute CLI dependency executables
/// </summary>
public interface ICliProcess
{
    /// <summary>
    /// Execute an executable with arguments at working directory
    /// </summary>
    /// <param name="executablePath">executable path</param>
    /// <param name="workingDirectory">working directory for this execution</param>
    /// <param name="arguments">arguments passed to executable</param>
    /// <param name="cancellationToken">token to cancel execution</param>
    /// <param name="environmentVariables">environment variable for this process</param>
    /// <param name="writeToStandardInput">Action to write to process input</param>
    /// <returns></returns>
    Task<string> ExecuteAsync(string executablePath, string workingDirectory, string[] arguments, CancellationToken cancellationToken, IReadOnlyDictionary<string, string>? environmentVariables=null, Action<StreamWriter>? writeToStandardInput = null);
}
