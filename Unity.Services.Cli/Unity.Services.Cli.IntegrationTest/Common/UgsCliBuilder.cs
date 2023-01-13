using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

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
        var pythonPath = await GetPythonPathAsync();

        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = pythonPath,
                WorkingDirectory = RootDirectory,
                Arguments = $"{buildScript} --extra-defines USE_MOCKSERVER_ENDPOINTS"
            },
        };

        foreach (var (key, value) in GetCliBuildEnvironmentVariables())
        {
            process.StartInfo.EnvironmentVariables[key] = value;
        }

        var (exitCode, _, error) = await process.GetProcessResultAsync();
        if (exitCode != 0)
        {
            throw new Exception(error);
        }
    }

    static async Task<string> GetPythonPathAsync()
    {
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = GetWhereIsCommandName(),
                WorkingDirectory = RootDirectory,
                Arguments = "python"
            },
        };

        var (exitCode, output, error) = await process.GetProcessResultAsync();
        if (exitCode != 0)
        {
            throw new Exception(error);
        }

        return output.Split("\r\n").First();

        static string GetWhereIsCommandName()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)
                || RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                return "whereis";

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return "where";

            throw new NotSupportedException("Current platform not supported");
        }
    }

    static IDictionary<string, string> GetCliBuildEnvironmentVariables()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            return new Dictionary<string, string>
            {
                ["SKIP_MACOS_BUILD"] = "1",
                ["SKIP_WINDOWS_BUILD"] = "1",
                ["SKIP_ALPINE_LINUX_BUILD"] = "1",
            };
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return new Dictionary<string, string>
            {
                ["SKIP_LINUX_BUILD"] = "1",
                ["SKIP_WINDOWS_BUILD"] = "1",
                ["SKIP_ALPINE_LINUX_BUILD"] = "1",
            };
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return new Dictionary<string, string>
            {
                ["SKIP_LINUX_BUILD"] = "1",
                ["SKIP_MACOS_BUILD"] = "1",
                ["SKIP_ALPINE_LINUX_BUILD"] = "1",
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
