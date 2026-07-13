using System.CommandLine;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Unity.Services.Cli.Authoring.Handlers;
using Unity.Services.Cli.Authoring.Input;
using Unity.Services.Cli.Common;
using Unity.Services.Cli.Common.Console;
using Unity.Services.Cli.Common.Input;
using Unity.Services.Cli.Authoring.Service;
using Unity.Services.Cli.Common.Telemetry.AnalyticEvent;
using Unity.Services.Cli.Common.Utils;

namespace Unity.Services.Cli.Authoring;

/// <summary>
/// Deploy Module to achieve services deploy command
/// </summary>
public class FetchModule : ICommandModule
{
    public Command? ModuleRootCommand { get; }

    public FetchModule()
    {
        ObfuscatedInputs.Instance.NonObfuscatedOptions.Add(FetchInput.ServiceOptions);
        ModuleRootCommand = new Command(
            "fetch",
            new CommandDescription("Fetch configuration files of supported services from the backend.")
                .WithReturn("List of fetched files grouped by status; or JSON list containing name, path and status with --json.")
                .Build())
        {
            FetchInput.PathArgument,
            FetchInput.ReconcileOption,
            FetchInput.ServiceOptions,
            FetchInput.DryRunOption,
            CommonInput.EnvironmentNameOption,
            CommonInput.CloudProjectIdOption,
        };
        ModuleRootCommand.SetHandler<
            IHost,
            FetchInput,
            IUnityEnvironment,
            ILogger,
            IAuthoringFileService,
            ILoadingIndicator,
            IAnalyticsEventBuilder,
            CancellationToken>(
            FetchCommandHandler.FetchAsync);
    }
}
