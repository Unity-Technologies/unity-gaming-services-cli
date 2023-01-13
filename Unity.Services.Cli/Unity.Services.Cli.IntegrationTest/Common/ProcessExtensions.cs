using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Unity.Services.Cli.IntegrationTest;

public static class ProcessExtensions
{
    /// <summary>
    /// Helper method to Start a process and return stdout, strerr and exit code.
    /// </summary>
    /// <param name="process">Process</param>
    /// <param name="cancellationToken">Cancellation token to stop waiting for the process to finish</param>
    /// <returns>A task returning an exit code, output and error</returns>
    /// <remarks>Cancelling the task does not exit the process, it only stops waiting for exit.</remarks>
    public static async Task<(int exitCode, string output, string error)> GetProcessResultAsync(this Process process, CancellationToken cancellationToken = default)
    {
        var output = new StringBuilder();
        var error = new StringBuilder();

        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.RedirectStandardError = true;
        process.StartInfo.UseShellExecute = false;
        process.EnableRaisingEvents = true;

        process.OutputDataReceived += (_, args) => output.AppendLine(args.Data);
        process.ErrorDataReceived += (_, args) => error.AppendLine(args.Data);

        process.Start();

        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        await process.WaitForExitAsync(cancellationToken);

        return (process.ExitCode, output.ToString(), error.ToString());
    }
}
