using System.CommandLine;
using System.IO.Abstractions;
using Microsoft.Extensions.Logging;
using Unity.Services.Cli.Common;
using Unity.Services.Cli.Deploy.Input;
using Unity.Services.Cli.Deploy.Templates;

namespace Unity.Services.Cli.Deploy.Handlers;

public static class NewFileHandler
{
    public static Command AddNewFileCommand<T>(this Command? self, string serviceName)
        where T : IFileTemplate
    {
        Command newFileCommand = new("new-file", $"Create new {serviceName} config file.")
        {
            NewFileInput.FileArgument
        };

        newFileCommand.SetHandler<NewFileInput, IFile, ILogger, CancellationToken>
        ((input, file, logger, token) => NewFileAsync(input, file, Activator.CreateInstance<T>(), logger, token));

        return newFileCommand;
    }

    public static async Task NewFileAsync<T>(
        NewFileInput input,
        IFile file,
        T template,
        ILogger logger,
        CancellationToken cancellationToken)
    where T : IFileTemplate
    {
        input.File = Path.ChangeExtension(input.File!, template.Extension);
        await file.WriteAllTextAsync(input.File, template.FileBodyText, cancellationToken);
        logger.LogInformation("Config file {input.File!} created successfully!", input.File!);
    }
}
