using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using Unity.Services.Cli.Common.Exceptions;

namespace Unity.Services.Cli.Common.Process;

public class CliProcess : ICliProcess
{
    public async Task<string> ExecuteAsync(string executablePath, string workingDirectory, string[] arguments, CancellationToken cancellationToken, IReadOnlyDictionary<string, string>? environmentVariables=null, Action<StreamWriter>? writeToStandardInput = null)
    {
        var argumentLine = string.Join(" ", arguments);
        var process = new System.Diagnostics.Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = executablePath,
                WorkingDirectory = workingDirectory,
                Arguments = argumentLine
            }
        };

        if (environmentVariables is not null)
        {
            foreach (var (key, value) in environmentVariables)
            {
                process.StartInfo.EnvironmentVariables[key] = value;
            }
        }

        try
        {
            var (exitCode, output, error) = await process.GetProcessResultAsync(writeToStandardInput, cancellationToken);
            if (exitCode != ExitCode.Success)
            {
                throw new ProcessException($"{process.StartInfo.FileName} {process.StartInfo.Arguments}:{Environment.NewLine} {error}");
            }

            return output;
        }
        catch (Win32Exception e)
        {
            throw new ProcessException($"Can not find {process.StartInfo.FileName}, please ensure its path is configured in PATH environment variable", e, Exceptions.ExitCode.HandledError);
        }
    }
}
