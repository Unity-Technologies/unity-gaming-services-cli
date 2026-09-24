using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using System.Net;
using System.Text;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using Unity.Services.Cli.Common.Input;
using Unity.Services.Cli.Common.Telemetry;
using Unity.Services.Cli.Common.Telemetry.AnalyticEvent;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using IdentityApiException = Unity.Services.Gateway.IdentityApiV1.Generated.Client.ApiException;
using CloudCodeApiException = Unity.Services.Gateway.CloudCodeApiV1.Generated.Client.ApiException;
using SchedulerApiException = Unity.Services.Gateway.SchedulerApiV1.Generated.Client.ApiException;
using CloudContentDeliveryApiException =
    Unity.Services.Gateway.ContentDeliveryManagementApiV1.Generated.Client.ApiException;
using LiveContentApiException = Unity.Services.Gateway.LiveContentApiV1.Generated.Client.ApiException;
using LobbyApiException = Unity.Services.MpsLobby.LobbyApiV1.Generated.Client.ApiException;
using LeaderboardApiException = Unity.Services.Gateway.LeaderboardApiV1.Generated.Client.ApiException;
using PlayerAdminApiException = Unity.Services.Gateway.PlayerAdminApiV3.Generated.Client.ApiException;
using PlayerAuthException = Unity.Services.Gateway.PlayerAuthApiV1.Generated.Client.ApiException;
using LiveReleasesApiException = Unity.Services.Gateway.LiveReleasesApiV1.Generated.Client.ApiException;
using CloudSaveApiException = Unity.Services.Gateway.CloudSaveApiV1.Generated.Client.ApiException;
using SchemaRegistryApiException = Unity.Services.Gateway.SchemaRegistryApiV1.Generated.Client.ApiException;

namespace Unity.Services.Cli.Common.Exceptions;

public partial class ExceptionHelper
{
    IAnalyticEvent Diagnostics { get; }
    readonly IAnsiConsole m_AnsiConsole;
    internal const string TroubleshootingHelp = "For help troubleshooting this error, visit this page in your browser";

    internal readonly IReadOnlyDictionary<HttpStatusCode, string> httpErrorTroubleshootingLinks =
        new Dictionary<HttpStatusCode, string>
        {
            [HttpStatusCode.Forbidden] =
                "https://services.docs.unity.com/guides/ugs-cli/latest/general/troubleshooting/unauthorized-error-403"
        };

    public ExceptionHelper(IAnalyticEvent diagnostics, IAnsiConsole ansiConsole)
    {
        Diagnostics = diagnostics;
        m_AnsiConsole = ansiConsole;
    }

    public int HandleException(Exception exception, ILogger logger, InvocationContext context, int depth = 0)
    {
        var cancellationToken = context.GetCancellationToken();
        if (cancellationToken.IsCancellationRequested)
        {
            context.ExitCode = ExitCode.Cancelled;
            return ExitCode.Cancelled;
        }

        var exitCode = ExitCode.HandledError;

        switch (exception)
        {
            case CliException cliException:
                if (cliException.ExitCode == ExitCode.HandledError)
                {
                    logger.LogError(cliException.Message);
                }
                else if (cliException.ExitCode == ExitCode.UnhandledError)
                {
                    ExecuteUnhandledExceptionFlow(exception, context, depth);
                    exitCode = ExitCode.UnhandledError;
                }
                break;
            case DeploymentFailureException deploymentFailureException:
                // We don't log this exception because the deployment content already
                // has all the information regarding any content failure
                context.ExitCode = deploymentFailureException.ExitCode;
                break;
            case IdentityApiException identityApiException:
                HandleApiException(exception, logger, identityApiException.ErrorCode);
                break;
            case CloudCodeApiException cloudCodeApiException:
                HandleApiException(exception, logger, cloudCodeApiException.ErrorCode);
                break;
            case SchedulerApiException schedulerApiException:
                HandleApiException(exception, logger, schedulerApiException.ErrorCode);
                break;
            case LiveContentApiException liveContentApiException:
                HandleApiException(exception, logger, liveContentApiException.ErrorCode);
                break;
            case CloudContentDeliveryApiException cloudContentDeliveryApiException:
                HandleApiException(exception, logger, cloudContentDeliveryApiException.ErrorCode);
                break;
            case CloudSaveApiException cloudSaveApiException:
                HandleApiException(exception, logger, cloudSaveApiException.ErrorCode);
                break;
            case LobbyApiException lobbyApiException:
                HandleApiException(exception, logger, lobbyApiException.ErrorCode);
                break;
            case LeaderboardApiException leaderboardApiException:
                HandleApiException(exception, logger, leaderboardApiException.ErrorCode);
                break;
            case PlayerAdminApiException playerAdminApiException:
                HandleApiException(exception, logger, playerAdminApiException.ErrorCode);
                break;
            case LiveReleasesApiException liveReleasesApiException:
                HandleApiException(exception, logger, liveReleasesApiException.ErrorCode);
                break;
            case PlayerAuthException playerAuthApiException:
                HandleApiException(exception, logger, playerAuthApiException.ErrorCode);
                break;
            case SchemaRegistryApiException schemaRegistryApiException:
                HandleApiException(exception, logger, schemaRegistryApiException.ErrorCode);
                break;
            case AggregateException aggregateException:
                foreach (var ex in aggregateException.InnerExceptions)
                {
                    var aggregateExitCode = HandleException(ex, logger, context, depth + 1);
                    if (aggregateExitCode > exitCode)
                    {
                        exitCode = aggregateExitCode;
                    }
                }

                if (depth == 0 && exitCode != ExitCode.HandledError)
                {
                    m_AnsiConsole.WriteException(exception);
                }

                context.ExitCode = exitCode;
                break;
            default:
                ExecuteUnhandledExceptionFlow(exception, context, depth);
                exitCode = ExitCode.UnhandledError;
                break;
        }

        context.ExitCode = exitCode;
        return exitCode;
    }

    void ExecuteUnhandledExceptionFlow(Exception exception, InvocationContext context, int depth)
    {
        if (depth == 0)
        {
            m_AnsiConsole.WriteException(exception);
        }

        try
        {
            Diagnostics.AddData(DiagnosticsTagKeys.DiagnosticName, "cli_unhandled_exception");
            Diagnostics.AddData(DiagnosticsTagKeys.DiagnosticMessage, exception.ToString());

            var cmdStr = GetCommandString(context);

            Diagnostics.AddData(DiagnosticsTagKeys.Command, cmdStr);
            Diagnostics.AddData(TagKeys.Timestamp, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());

            Diagnostics.Send();
        }
        catch
        {
            // Diagnostics sending failures should be silenced as to not interrupt execution
        }
    }

    static string GetCommandString(InvocationContext context)
    {
        var cmdResult = context.ParseResult.CommandResult;
        var currentCmd = cmdResult;
        var fullCommands = new List<string>();
        do
        {
            fullCommands.Add(currentCmd.Token.Value);
            currentCmd = currentCmd.Parent as CommandResult;
        } while (currentCmd != null);
        fullCommands.Reverse();
        var commandStringBuilder = new StringBuilder(string.Join("_", fullCommands));
        try
        {
            foreach (var child in cmdResult.Children)
            {
                var arg = child as ArgumentResult;
                var opt = child as OptionResult;
                string symbolName;
                if (arg != null)
                    symbolName = arg.Argument.Name;
                symbolName = opt?.Option.Aliases.FirstOrDefault() ?? opt?.Option.Name ?? child.Symbol.Name;

                var isException =
                    child is ArgumentResult && ObfuscatedInputs.Instance.NonObfuscatedArgs.Contains(arg!.Argument)
                    || child is OptionResult && ObfuscatedInputs.Instance.NonObfuscatedOptions.Contains(opt!.Option);
                if (!isException && arg != null)
                {
                    foreach (var _ in child.Tokens)
                    {
                        commandStringBuilder.Append("_" + $"(obf{symbolName})");
                    }
                }
                else if (!isException && opt != null)
                {
                    commandStringBuilder.Append("_" + opt.Token ?? child.Symbol.Name);
                    foreach (var _ in child.Tokens)
                    {
                        commandStringBuilder.Append("_" + $"(obf{symbolName})");
                    }
                }
                else
                {
                    if (child is OptionResult op)
                        commandStringBuilder.Append("_" + op.Token ?? child.Symbol.Name);
                    foreach (var token in child.Tokens)
                        commandStringBuilder.Append("_" + token.Value);
                }
            }
        }
        catch (Exception)
        {
            return string.Join("_", fullCommands) + "_failed_to_obfuscate";
        }
        return commandStringBuilder.ToString();
    }


    void HandleApiException(Exception exception, ILogger logger, int errorCode)
    {
        bool isErrorCodeRelatedToHttpStatus = Enum.IsDefined(typeof(HttpStatusCode), errorCode);
        HttpStatusCode? statusCode = isErrorCodeRelatedToHttpStatus ? (HttpStatusCode?)errorCode : null;
        string? troubleShootingLink = null;

        if (statusCode is not null)
        {
            httpErrorTroubleshootingLinks.TryGetValue(statusCode.Value, out troubleShootingLink);
        }

        var fullExceptionMessage = ParseApiException(exception, troubleShootingLink);

        logger.LogError(fullExceptionMessage);
    }

    /// <summary>
    /// Extracts and formats JSON error details from API exceptions, with fallback to original message.
    /// </summary>
    internal static string ParseApiException(Exception exception, string? troubleShootingLink)
    {
        var fallback = troubleShootingLink is null
            ? exception.Message
            : string.Join(
                Environment.NewLine,
                exception.Message,
                TroubleshootingHelp,
                troubleShootingLink);

        var message = exception.Message;
        if (string.IsNullOrWhiteSpace(message)) return fallback;

        message = message.Trim();
        var match = k_ApiExceptionPattern.Match(message);
        if (!match.Success) return fallback;

        message = message[match.Length..];

        try
        {
            var jsonObject = JObject.Parse(message);
            if (troubleShootingLink != null)
            {
                jsonObject[TroubleshootingHelp] = troubleShootingLink;
            }

            jsonObject["Error calling"] = jsonObject["Error calling"] = match.Groups[1].Value;
            return jsonObject.ToString();
        }
        catch (Exception)
        {
            return fallback;
        }
    }

    static readonly Regex k_ApiExceptionPattern = ApiExceptionMessagePattern();

    [GeneratedRegex(@"^Error calling (\w+): ", RegexOptions.Compiled)]
    private static partial Regex ApiExceptionMessagePattern();
}
