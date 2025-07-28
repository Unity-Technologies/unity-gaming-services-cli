using System.Diagnostics;

namespace Unity.Services.ShellCommand.MockCli
{
    static class Program
    {
        static async Task<int> Main(string[] args)
        {
            var completionSource = new TaskCompletionSource();
            var exitCode = 0;

            if (args.Length > 0)
            {
                var ok = int.TryParse(args[0], out exitCode);
                if (!ok)
                {
                    return 255;
                }
            }

            Console.WriteLine("Mock process is running with PID " + Environment.ProcessId);
            Console.WriteLine("Process Name: " + Process.GetCurrentProcess().ProcessName);
            Console.WriteLine("--------------------------------------");
            // IMPORTANT: Do not delete the following log, because we are waiting for it in the tests
            Console.WriteLine("Waiting for SIGINT (Ctrl + C) to exit");
            Console.WriteLine($"Process will exit with code {exitCode}");

            Console.CancelKeyPress += (_, e) =>
            {
                // We avoid terminating the process
                e.Cancel = true;

                Console.WriteLine("SIGINT (Ctrl + C) received");
                completionSource.SetResult();
            };

            // This ensures the process terminates eventually instead of leaking memory. Just in case...
            using var cancelSource = new CancellationTokenSource();
            cancelSource.CancelAfter(TimeSpan.FromMinutes(1));

            try
            {
                await completionSource.Task.WaitAsync(cancelSource.Token);
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("Timeout reached, exiting");
                exitCode = 254;
            }

            Console.WriteLine("Ok, bye");
            return exitCode;
        }
    }
}

