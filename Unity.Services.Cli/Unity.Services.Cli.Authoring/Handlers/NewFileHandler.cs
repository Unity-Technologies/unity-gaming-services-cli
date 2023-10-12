using System.CommandLine;
using System.IO.Abstractions;
using Microsoft.Extensions.Logging;
using Unity.Services.Cli.Authoring.Input;
using Unity.Services.Cli.Authoring.Templates;
using Unity.Services.Cli.Common;
using Unity.Services.Cli.Common.Input;

namespace Unity.Services.Cli.Authoring.Handlers;

public static class NewFileHandler
{
    public static Command AddNewFileCommand<T>(this Command? self, string serviceName, string defaultFileName = "new_file")
        where T : IFileTemplate, new()
    {
        Command newFileCommand = new("new-file", $"Create new {serviceName} config file.")
        {
            NewFileInput.FileArgument,
            CommonInput.UseForceOption
        };

        newFileCommand.SetHandler<NewFileInput, IFile, ILogger, CancellationToken>
        ((input, file, logger, token) => NewFileAsync(input, file, new T(), logger, token, defaultFileName));

        return newFileCommand;
    }

    public static async Task NewFileAsync<T>(
        NewFileInput input,
        IFile file,
        T template,
        ILogger logger,
        CancellationToken cancellationToken,
        string defaultFileName = "new_file")
    where T : IFileTemplate
    {
        input.File = Path.ChangeExtension(input.File ?? defaultFileName, template.Extension);

        if (file.Exists(input.File) && !input.UseForce)
        {
            logger.LogError($"A file with the name '{input.File}' already exists." +
                            " Add --force to overwrite the file.");
        }
        else
        {
            await file.WriteAllTextAsync(input.File, template.FileBodyText, cancellationToken);
            logger.LogInformation("Config file {input.File!} created successfully!", input.File!);
        }
    }
}
