using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Unity.Services.Cli.Common.Process;

namespace Unity.Services.Cli.IntegrationTest;

/// <summary>
/// A class to allow the build of the CLI binary
/// </summary>
public static class UgsCliBuilder
{
    /// <summary>
    /// The relative path to the root directory of this project
    /// </summary>
    public static readonly string RootDirectory = Path.GetFullPath("../../../../..");

    /// <summary>
    /// The binary path, relative to <see cref="RootDirectory"/>
    /// </summary>
    public static string CliPath { get; } = GetCliPath();

    /// <summary>
    /// Build the CLI binary using the build script from this repository.
    /// </summary>
    /// <exception cref="Exception">
    /// Exception when the build fails.
    /// </exception>
    public static async Task Build()
    {
        const string buildScript = "build.py";
        var pythonPath = "python3";
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            pythonPath = await GetWindowsPythonPathAsync();
        }

        var process = new CliProcess();
        var environmentVariables = GetCliBuildEnvironmentVariables();
        await process.ExecuteAsync(pythonPath, RootDirectory, new[]
        {
            buildScript,
            "--extra-defines USE_MOCKSERVER_ENDPOINTS"
        }, CancellationToken.None, environmentVariables);
    }

    static async Task<string> GetWindowsPythonPathAsync()
    {
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "where",
                WorkingDirectory = RootDirectory,
                Arguments = "python"
            },
        };

        var (exitCode, output, error) = await process.GetProcessResultAsync();
        if (exitCode != 0)
        {
            throw new Exception($"{process.StartInfo.FileName} {process.StartInfo.Arguments}: {error}");
        }
        return output.Split("\r\n").First();
    }

    static IReadOnlyDictionary<string, string> GetCliBuildEnvironmentVariables()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            return new Dictionary<string, string>
            {
                ["SKIP_MACOS_BUILD"] = "1",
                ["SKIP_WINDOWS_BUILD"] = "1",
                ["SKIP_LINUX_MUSL_BUILD"] = "1",
            };
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return new Dictionary<string, string>
            {
                ["SKIP_LINUX_BUILD"] = "1",
                ["SKIP_WINDOWS_BUILD"] = "1",
                ["SKIP_LINUX_MUSL_BUILD"] = "1",
            };
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return new Dictionary<string, string>
            {
                ["SKIP_LINUX_BUILD"] = "1",
                ["SKIP_MACOS_BUILD"] = "1",
                ["SKIP_LINUX_MUSL_BUILD"] = "1",
            };
        }

        throw new NotSupportedException("Current platform not supported");
    }

    static string GetCliPath()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            return Path.Combine(RootDirectory, "build/linux/ugs");
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return Path.Combine(RootDirectory, "build/macos/ugs");
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return Path.Combine(RootDirectory, "build/windows/ugs.exe");
        }

        throw new NotSupportedException("Current platform not supported");
    }
}
