using Unity.Services.Cli.CloudCode.Input;
using Unity.Services.Cli.CloudCode.Parameters;
using Unity.Services.Cli.Common.Exceptions;
using CloudCodeAuthoringLanguage = Unity.Services.CloudCode.Authoring.Editor.Core.Model.Language;

namespace Unity.Services.Cli.CloudCode.Service;

class CloudCodeInputParser : ICloudCodeInputParser
{
    public ICloudCodeScriptParser CloudCodeScriptParser { get; }

    public CloudCodeInputParser(ICloudCodeScriptParser cloudCodeScriptParser)
    {
        CloudCodeScriptParser = cloudCodeScriptParser;
    }

    public static readonly Dictionary<CloudCodeAuthoringLanguage, string> Extensions = new()
    {
        [CloudCodeAuthoringLanguage.JS] = "js"
    };
    public string ParseLanguage(CloudCodeInput input)
    {
        if (string.IsNullOrEmpty(input.ScriptLanguage))
        {
            return "JS";
        }

        return input.ScriptLanguage;
    }

    public string ParseScriptType(CloudCodeInput input)
    {
        if (string.IsNullOrEmpty(input.ScriptType))
        {
            return "API";
        }

        return input.ScriptType;
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
            throw new CliException(
                string.Join(
                    " ",
                    exception.Message,
                    $"The path passed is not a valid file path, please review it and try again."),
                ExitCode.HandledError);
        }
        catch (IOException exception)
        {
            throw new CliException(
                string.Join(
                    " ",
                    exception.Message,
                    $"The file path passed could not be found or is incomplete."),
                ExitCode.HandledError);
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
