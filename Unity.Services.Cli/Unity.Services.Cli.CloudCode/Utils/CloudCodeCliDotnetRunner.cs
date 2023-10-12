using Unity.Services.Cli.Common.Process;
using Unity.Services.CloudCode.Authoring.Editor.Core.Dotnet;

namespace Unity.Services.Cli.CloudCode.Utils;

class CloudCodeCliDotnetRunner : IDotnetRunner
{
    ICliProcess m_CliProcess;

    public CloudCodeCliDotnetRunner(ICliProcess cliProcess)
    {
        m_CliProcess = cliProcess;
    }

    public async Task<bool> IsDotnetAvailable()
    {
        try
        {
            await m_CliProcess.ExecuteAsync(
                "dotnet",
                System.Environment.CurrentDirectory,
                new[]
                {
                    "--version"
                },
                CancellationToken.None);
        }
        catch (ProcessException)
        {
            return false;
        }

        return true;
    }

    public async Task<string> ExecuteDotnetAsync(IEnumerable<string> arguments, CancellationToken cancellationToken)
    {
        try
        {
            return await m_CliProcess.ExecuteAsync(
                "dotnet",
                System.Environment.CurrentDirectory,
                arguments.ToArray(),
                cancellationToken);
        }
        catch (ProcessException e)
        {
            throw new DotnetCommandFailedException(e.Message);
        }
    }
}
