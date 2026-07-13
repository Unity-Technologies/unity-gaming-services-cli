using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Unity.Services.Cli.IntegrationTest.Common;

public partial class UgsCliTestCase
{
    class ExternalProcess : IProcess
    {
        const int k_Timeout = 45;

        bool m_HasWaited;

        public ExternalProcess(Process innerProcess)
        {
            InnerProcess = innerProcess;
            StandardOutput = null!;
            StandardError = null!;
        }

        public Process InnerProcess { get; }

        public bool HasExited => InnerProcess.HasExited;
        public async Task WaitForExitAsync(CancellationToken cancellationToken = default)
        {
            if (m_HasWaited)
                return;

            var stdoutTask = InnerProcess.StandardOutput.ReadToEndAsync(cancellationToken);
            var stderrTask = InnerProcess.StandardError.ReadToEndAsync(cancellationToken);

            try
            {
                await InnerProcess
                    .WaitForExitAsync(cancellationToken)
                    .WaitAsync(TimeSpan.FromSeconds(k_Timeout), cancellationToken);
            }
            catch (TimeoutException)
            {
                InnerProcess.Kill();
            }

            StandardOutput = new StreamReader(new MemoryStream(Encoding.UTF8.GetBytes(await stdoutTask)));
            StandardError = new StreamReader(new MemoryStream(Encoding.UTF8.GetBytes(await stderrTask)));
            m_HasWaited = true;
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
        public StreamReader StandardOutput { get; private set; }
        public StreamReader StandardError { get; private set; }
    }
}
