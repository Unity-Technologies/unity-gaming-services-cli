using System.IO.Abstractions;
using Microsoft.Extensions.Logging;
using Unity.Services.Cli.CloudCode.Input;
using Unity.Services.Cli.Common.Console;
using Unity.Services.CloudCode.Authoring.Editor.Core.Solution;

namespace Unity.Services.Cli.CloudCode.Handlers.NewFile;

static class NewFileModuleHandler
{
    public static async Task CreateNewModule(
        CloudCodeInput input,
        IPath path,
        IDirectory directory,
        CloudCodeModuleSolutionGenerator solutionGenerator,
        ILogger logger,
        ILoadingIndicator loadingIndicator,
        CancellationToken cancellationToken)
    {
        await loadingIndicator.StartLoadingAsync(
            "Creating Solution",
            _ => CreateNewModuleInternal(input, path, directory, solutionGenerator, logger, cancellationToken));
    }

    static async Task CreateNewModuleInternal(
        CloudCodeInput input,
        IPath path,
        IDirectory directory,
        CloudCodeModuleSolutionGenerator solutionGenerator,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var parentDirectory = input.ModuleDirectory ?? System.Environment.CurrentDirectory;
        var moduleName = input.ModuleName ?? "NewCloudCodeModule";
        var moduleDirectory = path.Join(parentDirectory, moduleName);

        var directoryExists = directory.Exists(moduleDirectory);

        if (directoryExists && !input.UseForce)
        {
            logger.LogError($"A Cloud Code Module at path '{moduleDirectory}' already exists." +
                " Add --force to overwrite the Cloud Code Module.");
        }
        else
        {
            if (directoryExists)
            {
                directory.Delete(moduleDirectory, true);
            }

            try
            {
                await solutionGenerator.CreateSolutionWithProject(moduleDirectory, moduleName, cancellationToken);
                logger.LogInformation($"Module '{moduleName}' created successfully at path '{moduleDirectory}'!", moduleName);
            }
            catch (Exception e)
            {
                logger.LogError(e.Message);
                throw;
            }
        }
    }
}
