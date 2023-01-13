using System.CommandLine;
using System.IO.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Unity.Services.Cli.Common;
using Unity.Services.Cli.Common.Console;
using Unity.Services.Cli.Deploy.Input;
using Unity.Services.Cli.Deploy.Handlers;
using Unity.Services.Cli.Deploy.Service;

namespace Unity.Services.Cli.Deploy;

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
            DeployInput.PathsArgument
        };
        ModuleRootCommand.SetHandler<
            IHost,
            DeployInput,
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
        serviceCollection.AddTransient<IDeployFileService, DeployFileService>();
    }
}
