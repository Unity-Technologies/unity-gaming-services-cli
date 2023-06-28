using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Unity.Services.Cli.Common.Logging;

namespace Unity.Services.Cli.IntegrationTest.Common;

public partial class UgsCliTestCase
{
    class LocalProcess : IProcess
    {
        Task<int>? m_InnerTask;
        MemoryStream m_StdOutWriter;
        MemoryStream m_StdErrWriter;

        public LocalProcess(ProcessStartInfo info)
        {
            StartInfo = info;
            StandardError = null!;
            StandardOutput = null!;
            m_StdOutWriter = null!;
            m_StdErrWriter = null!;
        }

        public bool HasExited { get; private set; }
        public async Task WaitForExitAsync(CancellationToken cancellationToken = default)
        {
            if (m_InnerTask == null)
                throw new InvalidOperationException("Local 'Main' has not been started");
            if (HasExited)
                return;
            ExitCode = await m_InnerTask!;
            await m_StdErrWriter.FlushAsync(cancellationToken);
            await m_StdOutWriter.FlushAsync(cancellationToken);

            m_StdErrWriter.Position = 0;
            m_StdOutWriter.Position = 0;
            HasExited = true;
        }

        public int ExitCode { get; private set; }

        public ProcessStartInfo StartInfo { get; }

        public bool Start()
        {
            if (m_InnerTask != null)
                throw new InvalidOperationException("Local process was already started");

            //This split method does not account for \" at the moment
            var args = StartInfo.Arguments.Split(
                ' ',
                StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

            Environment.CurrentDirectory = UgsCliBuilder.RootDirectory;

            m_StdOutWriter= new MemoryStream(32*1024);
            m_StdErrWriter = new MemoryStream(32*1024);

            var stdOutWrite = new StreamWriter(m_StdOutWriter) { AutoFlush = true };
            var stdErrWrite = new StreamWriter(m_StdErrWriter){ AutoFlush = true };
            var logger = new Logger
            {
                StdOut = stdOutWrite,
                StdErr = stdErrWrite
            };

            StandardOutput = new StreamReader(m_StdOutWriter);
            StandardError = new StreamReader(m_StdErrWriter);

            m_InnerTask = Program.InternalMain(args, logger);
            return true;
        }

        public void Dispose()
        {
            StandardOutput.Dispose();
            StandardInput.Dispose();
        }

        public StreamWriter StandardInput
            => throw new NotSupportedException("stdin is not currently supported for local process");
        public StreamReader StandardOutput { get; private set; }
        public StreamReader StandardError { get; private set; }
    }
}
