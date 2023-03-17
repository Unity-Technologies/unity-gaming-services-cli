using Unity.Services.Cli.CloudCode.Input;
using Unity.Services.Cli.Common.Exceptions;
using Unity.Services.Gateway.CloudCodeApiV1.Generated.Model;
using Language = Unity.Services.Gateway.CloudCodeApiV1.Generated.Model.Language;
using CloudCodeAuthoringLanguage = Unity.Services.CloudCode.Authoring.Editor.Core.Model.Language;

namespace Unity.Services.Cli.CloudCode.Service;

class CloudCodeInputParser : ICloudCodeInputParser
{
    public static readonly Dictionary<CloudCodeAuthoringLanguage, string> Extensions = new()
    {
        [CloudCodeAuthoringLanguage.JS] = "js"
    };
    public Language ParseLanguage(CloudCodeInput input)
    {
        if (string.IsNullOrEmpty(input.ScriptLanguage))
        {
            return Language.JS;
        }

        try
        {
            return Enum.Parse<Language>(input.ScriptLanguage);
        }
        catch (ArgumentException)
        {
            var languages = String.Join(",", Enum.GetNames<Language>());
            throw new CliException($"'{input.ScriptLanguage}' is not a valid {nameof(Language)}." +
                                   $" Valid {nameof(Language)}: " + languages + ".", ExitCode.HandledError);
        }
    }

    public ScriptType ParseScriptType(CloudCodeInput input)
    {
        if (string.IsNullOrEmpty(input.ScriptType))
        {
            return ScriptType.API;
        }

        try
        {
            return Enum.Parse<ScriptType>(input.ScriptType);
        }
        catch (ArgumentException)
        {
            var types = String.Join(",", Enum.GetNames<ScriptType>());
            throw new CliException($"'{input.ScriptType}' is not a valid {nameof(ScriptType)}." +
                                   $" Valid {nameof(ScriptType)}: " + types + ".", ExitCode.HandledError);
        }
    }

    public async Task<string> LoadScriptCodeAsync(CloudCodeInput input, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(input.FilePath))
        {
            throw new CliException("The file path provided is null or empty." +
                                   " Please enter a valid file path.", ExitCode.HandledError);
        }

        return await LoadScriptCodeAsync(input.FilePath, cancellationToken);
    }

    public async Task<string> LoadScriptCodeAsync(string filePath, CancellationToken cancellationToken)
    {
        try
        {
            return await File.ReadAllTextAsync(filePath, cancellationToken);
        }
        catch (FileNotFoundException exception)
        {
            throw new CliException(exception.Message, ExitCode.HandledError);
        }
        catch (UnauthorizedAccessException exception)
        {
            throw new CliException(string.Join(" ", exception.Message,
                "Make sure that the CLI has the permissions to access the file and that the " +
                "specified path points to a file and not a directory."), ExitCode.HandledError);
        }
    }

    public async Task<Stream> LoadModuleContentsAsync(string filePath)
    {
        try
        {
            return await Task.FromResult<Stream>(File.Open(filePath, FileMode.Open, FileAccess.Read));
        }
        catch (FileNotFoundException exception)
        {
            throw new CliException(exception.Message, ExitCode.HandledError);
        }
        catch (UnauthorizedAccessException exception)
        {
            throw new CliException(string.Join(" ", exception.Message,
                "Make sure that the CLI has the permissions to access the file and that the " +
                "specified path points to a file and not a directory."), ExitCode.HandledError);
        }
    }
}
