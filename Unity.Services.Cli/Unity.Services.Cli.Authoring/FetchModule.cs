using System.CommandLine;
using System.IO.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Unity.Services.Cli.Authoring.Handlers;
using Unity.Services.Cli.Authoring.Input;
using Unity.Services.Cli.Common;
using Unity.Services.Cli.Common.Console;
using Unity.Services.Cli.Common.Input;
using Unity.Services.Cli.Authoring.Service;

namespace Unity.Services.Cli.Authoring;

/// <summary>
/// Deploy Module to achieve services deploy command
/// </summary>
public class FetchModule : ICommandModule
{
    public Command? ModuleRootCommand { get; }

    public FetchModule()
    {
        ModuleRootCommand = new Command(
            "fetch",
            $"Fetch configuration files of supported services from the backend.")
        {
            FetchInput.PathArgument,
            FetchInput.ReconcileOption,
            FetchInput.ServiceOptions,
            FetchInput.DryRunOption,
            CommonInput.EnvironmentNameOption,
            CommonInput.CloudProjectIdOption
        };
        ModuleRootCommand.SetHandler<
            IHost,
            FetchInput,
            ILogger,
            ILoadingIndicator,
            CancellationToken>(
            FetchHandler.FetchAsync);
    }
}
