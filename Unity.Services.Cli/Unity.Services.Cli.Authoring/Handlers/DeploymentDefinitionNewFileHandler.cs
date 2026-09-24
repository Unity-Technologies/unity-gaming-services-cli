using System.CommandLine;
using System.IO.Abstractions;
using Microsoft.Extensions.Logging;
using Unity.Services.Cli.Authoring.Input;
using Unity.Services.Cli.Authoring.Model;
using Unity.Services.Cli.Common;
using Unity.Services.Cli.Common.Input;

namespace Unity.Services.Cli.Authoring.Handlers;

static class DeploymentDefinitionNewFileHandler
{
    public static Command CreateNewFileCommand()
    {
        Command newFileCommand = new("new-file", "Create a new Deployment Definition file.")
        {
            NewFileInput.FileArgument,
            CommonInput.UseForceOption
        };

        newFileCommand.SetHandler<NewFileInput, IFile, IDirectory, IPath, ILogger, CancellationToken>(NewFileAsync);

        return newFileCommand;
    }

    internal static async Task NewFileAsync(
        NewFileInput input,
        IFile file,
        IDirectory directory,
        IPath path,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var fileName = input.File ?? DDefConstants.DefaultFileName;
        var nameWithoutExtension = path.GetFileNameWithoutExtension(fileName);

        input.File = path.ChangeExtension(fileName, DDefConstants.Extension);

        var directoryPath = path.GetDirectoryName(input.File);
        if (!string.IsNullOrEmpty(directoryPath) && !directory.Exists(directoryPath))
        {
            directory.CreateDirectory(directoryPath);
        }

        if (file.Exists(input.File) && !input.UseForce)
        {
            logger.LogError(
                "A file with the name '{file}' already exists. Add --force to overwrite the file.",
                input.File);
        }
        else
        {
            var ddef = CliDeploymentDefinition.CreateTemplate(nameWithoutExtension);
            await file.WriteAllTextAsync(input.File, ddef.Serialize(), cancellationToken);
            logger.LogInformation("Config file {file} created successfully!", input.File);
        }
    }
}
