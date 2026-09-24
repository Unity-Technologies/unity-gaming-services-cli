using System.CommandLine;
using System.IO.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Unity.Services.Cli.Authoring.Handlers;
using Unity.Services.Cli.Common;

namespace Unity.Services.Cli.Authoring;

/// <summary>
/// Module for config-as-code utilities such as deployment definition scaffolding.
/// </summary>
public class ConfigAsCodeModule : ICommandModule
{
    /// <inheritdoc />
    public Command? ModuleRootCommand { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigAsCodeModule"/> class.
    /// </summary>
    public ConfigAsCodeModule()
    {
        ModuleRootCommand = new Command(
            "config-as-code",
            "Manage config-as-code files such as Deployment Definitions.")
        {
            DeploymentDefinitionNewFileHandler.CreateNewFileCommand()
        };

        ModuleRootCommand.AddAlias("cac");
    }

    /// <summary>
    /// Register services to UGS CLI host builder.
    /// </summary>
    public static void RegisterServices(HostBuilderContext hostBuilderContext, IServiceCollection serviceCollection)
    {
        serviceCollection.AddTransient<IFile>(_ => new FileSystem().File);
        serviceCollection.AddTransient<IDirectory>(_ => new FileSystem().Directory);
        serviceCollection.AddTransient<IPath>(_ => new FileSystem().Path);
    }
}
