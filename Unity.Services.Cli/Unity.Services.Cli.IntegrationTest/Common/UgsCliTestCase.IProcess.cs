using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Unity.Services.Cli.IntegrationTest.Common;

public partial class UgsCliTestCase
{
    interface IProcess
    {
        bool HasExited { get; }
        Task WaitForExitAsync(CancellationToken cancellationToken = default);
        StreamWriter StandardInput { get; }
        public StreamReader StandardOutput { get; }
        public StreamReader StandardError { get; }
        int ExitCode { get; }
        ProcessStartInfo StartInfo { get; }
        bool Start();
        void Dispose();
    }
}
