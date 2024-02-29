using Newtonsoft.Json;
using Unity.Services.Cli.Common.Exceptions;
using Unity.Services.Gateway.AccessApiV1.Generated.Model;
using Unity.Services.Gateway.GameServerHostingApiV1.Generated.Client;

namespace Unity.Services.Cli.GameServerHosting.Services;

static class ApiExceptionConverter
{
    /// <summary>
    /// It converts the ApiException to CliException and throws it, if .ErrorContent contains a valid JSON
    /// </summary>
    /// <param name="e"></param>
    /// <exception cref="CliException"></exception>
    public static void Convert(ApiException e)
    {
        if (e.ErrorContent == null)
        {
            throw e;
        }

        var validationError = JsonConvert.DeserializeObject<ValidationError>((string)e.ErrorContent);
        // if there is no detail, print out the whole error content
        if (validationError!.Detail != null)
        {
            throw new CliException($"{validationError.Detail}", e, ExitCode.HandledError);
        }

        throw new CliException($"{e.ErrorContent}", e, ExitCode.HandledError);
    }
}
