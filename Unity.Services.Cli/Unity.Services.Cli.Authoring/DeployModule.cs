using System.CommandLine;
using System.IO.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Unity.Services.Cli.Authoring.Handlers;
using Unity.Services.Cli.Authoring.Input;
using Unity.Services.Cli.Authoring.Service;
using Unity.Services.Cli.Common;
using Unity.Services.Cli.Common.Console;
using Unity.Services.Cli.Common.Input;
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
            $"Deploy configuration files of supported services to the backend.{Environment.NewLine}"
            + "Services currently supported are: remote-config, cloud-code.")
        {
            DeployInput.PathsArgument,
            DeployInput.ReconcileOption,
            DeployInput.DryRunOption,
            CommonInput.EnvironmentNameOption,
            CommonInput.CloudProjectIdOption
        };
        ModuleRootCommand.SetHandler<
            IHost,
            DeployInput,
            IDeployFileService,
            IUnityEnvironment,
            ILogger,
            ILoadingIndicator,
            CancellationToken>(
            DeployHandler.DeployAsync);
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
    }
}
