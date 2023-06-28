using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Unity.Services.Cli.IntegrationTest.Common;

public partial class UgsCliTestCase
{
    class ExternalProcess : IProcess
    {
        public ExternalProcess(Process innerProcess)
        {
            InnerProcess = innerProcess;
        }

        public Process InnerProcess { get; }

        public bool HasExited => InnerProcess.HasExited;
        public Task WaitForExitAsync(CancellationToken cancellationToken = default)
        {
            return InnerProcess.WaitForExitAsync(cancellationToken);
        }

        public int ExitCode => InnerProcess.ExitCode;

        public ProcessStartInfo StartInfo => InnerProcess.StartInfo;
        public bool Start()
        {
            return InnerProcess.Start();
        }

        public void Dispose()
        {
            InnerProcess.Dispose();
        }

        public StreamWriter StandardInput => InnerProcess.StandardInput;
        public StreamReader StandardOutput => InnerProcess.StandardOutput;
        public StreamReader StandardError => InnerProcess.StandardError;
    }
}
