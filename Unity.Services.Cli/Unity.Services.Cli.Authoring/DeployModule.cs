using System.CommandLine;
using System.IO.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Unity.Services.Cli.Authoring.DeploymentDefinition;
using Unity.Services.Cli.Authoring.Compression;
using Unity.Services.Cli.Authoring.Handlers;
using Unity.Services.Cli.Authoring.Input;
using Unity.Services.Cli.Authoring.Service;
using Unity.Services.Cli.Common;
using Unity.Services.Cli.Common.Console;
using Unity.Services.Cli.Common.Input;
using Unity.Services.Cli.Common.Telemetry.AnalyticEvent;
using Unity.Services.Cli.Common.Utils;

namespace Unity.Services.Cli.Authoring;

/// <summary>
/// Deploy Module to achieve services deploy command
/// </summary>
public class DeployModule : ICommandModule
{
    public Command? ModuleRootCommand { get; }

    public DeployModule()
    {
        ModuleRootCommand = new Command(
            "deploy",
            new CommandDescription("Deploy configuration files of supported services to the backend.")
                .WithReturn("List of deployed files grouped by status; or JSON list containing name, path and status with --json.")
                .Build())
        {
            DeployInput.PathsArgument,
            DeployInput.ReconcileOption,
            DeployInput.DryRunOption,
            DeployInput.ServiceOptions,
            CommonInput.EnvironmentNameOption,
            CommonInput.CloudProjectIdOption,
        };

        ObfuscatedInputs.Instance.NonObfuscatedOptions.Add(DeployInput.ServiceOptions);

        ModuleRootCommand.SetHandler<
            IHost,
            DeployInput,
            IUnityEnvironment,
            ILogger,
            ILoadingIndicator,
            IAuthoringFileService,
            IAnalyticsEventBuilder,
            CancellationToken>(
            DeployCommandHandler.DeployAsync);
    }

    /// <summary>
    /// Register service to UGS CLI host builder
    /// </summary>
    public static void RegisterServices(HostBuilderContext hostBuilderContext, IServiceCollection serviceCollection)
    {
        serviceCollection.AddTransient<IFile>(_ => new FileSystem().File);
        serviceCollection.AddTransient<IDirectory>(_ => new FileSystem().Directory);
        serviceCollection.AddTransient<IPath>(_ => new FileSystem().Path);
        serviceCollection.AddTransient<IDeployFileService, DeployFileService>();
        serviceCollection.AddTransient<IAuthoringFileService, AuthoringFileService>();
        serviceCollection.AddTransient<IFileDiscoveryService, FileDiscoveryService>();
        serviceCollection.AddTransient<IDeploymentDefinitionFactory, DeploymentDefinitionFactory>();
        serviceCollection.AddTransient<IZipArchiver, ZipArchiver>();
    }
}
