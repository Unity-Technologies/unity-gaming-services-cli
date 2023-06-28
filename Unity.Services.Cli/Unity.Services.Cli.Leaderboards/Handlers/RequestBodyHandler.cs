using Unity.Services.Cli.Common.Exceptions;

namespace Unity.Services.Cli.Leaderboards.Handlers;

class RequestBodyHandler
{
    public static async Task<string> GetRequestBodyAsync(string? input)
    {
        if (File.Exists(input))
        {
            return await File.ReadAllTextAsync(input);
        }

        throw new CliException("Invalid file path.", ExitCode.HandledError);
    }

}
