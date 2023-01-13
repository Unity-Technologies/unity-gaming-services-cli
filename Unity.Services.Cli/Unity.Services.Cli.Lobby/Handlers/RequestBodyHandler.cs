using Unity.Services.Cli.Common.Exceptions;

namespace Unity.Services.Cli.Lobby.Handlers
{
    internal static class RequestBodyHandler
    {
        internal static string GetRequestBodyFromFileOrInput(string? input, bool isRequired = false)
        {
            // Read the content from the file if the user provided a file path.
            if (File.Exists(input))
            {
                using var sr = new StreamReader(input);
                return sr.ReadToEnd().Trim('\r', '\n');
            }
            else // Otherwise, just default to using the raw input string.
            {
                if (isRequired && string.IsNullOrEmpty(input))
                {
                    throw new CliException("Required request body cannot be empty.", ExitCode.HandledError);
                }

                return input ?? string.Empty;
            }
        }
    }
}
