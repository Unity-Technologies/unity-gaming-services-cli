using System.CommandLine.Invocation;
using System.Net;
using System.Text;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using Unity.Services.Cli.Common.Telemetry;
using Unity.Services.Cli.Common.Telemetry.AnalyticEvent;
using IdentityApiException = Unity.Services.Gateway.IdentityApiV1.Generated.Client.ApiException;
using CloudCodeApiException = Unity.Services.Gateway.CloudCodeApiV1.Generated.Client.ApiException;
using SchedulerApiException = Unity.Services.Gateway.SchedulerApiV1.Generated.Client.ApiException;
using CloudContentDeliveryApiException = Unity.Services.Gateway.ContentDeliveryManagementApiV1.Generated.Client.ApiException;
using EconomyApiException = Unity.Services.Gateway.EconomyApiV2.Generated.Client.ApiException;
using LobbyApiException = Unity.Services.MpsLobby.LobbyApiV1.Generated.Client.ApiException;
using LeaderboardApiException = Unity.Services.Gateway.LeaderboardApiV1.Generated.Client.ApiException;
using PlayerAdminApiException = Unity.Services.Gateway.PlayerAdminApiV3.Generated.Client.ApiException;
using PlayerAuthException = Unity.Services.Gateway.PlayerAuthApiV1.Generated.Client.ApiException;
using HostingApiException = Unity.Services.Gateway.GameServerHostingApiV1.Generated.Client.ApiException;

namespace Unity.Services.Cli.Common.Exceptions;

public class ExceptionHelper
{
    IAnalyticEvent Diagnostics { get; }
    readonly IAnsiConsole m_AnsiConsole;
    internal const string TroubleshootingHelp = "For help troubleshooting this error, visit this page in your browser:";
    internal readonly IReadOnlyDictionary<HttpStatusCode, string> HttpErrorTroubleshootingLinks = new Dictionary<HttpStatusCode, string>
    {
        [HttpStatusCode.Forbidden] = "https://services.docs.unity.com/guides/ugs-cli/latest/general/troubleshooting/unauthorized-error-403"
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
            case CloudContentDeliveryApiException cloudContentDeliveryApiException:
                HandleApiException(exception, logger, cloudContentDeliveryApiException.ErrorCode);
                break;
            case EconomyApiException economyApiException:
                HandleApiException(exception, logger, economyApiException.ErrorCode);
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
            case PlayerAuthException playerAuthApiException:
                HandleApiException(exception, logger, playerAuthApiException.ErrorCode);
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
            case HostingApiException hostingException:
                HandleApiException(exception, logger, hostingException.ErrorCode);
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

            var command = new StringBuilder("ugs");
            foreach (var arg in context.ParseResult.Tokens)
            {
                command.Append("_" + arg);
            }
            Diagnostics.AddData(DiagnosticsTagKeys.Command, command.ToString());
            Diagnostics.AddData(TagKeys.Timestamp, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());

            Diagnostics.Send();
        }
        catch
        {
            // Diagnostics sending failures should be silenced as to not interrupt execution
        }
    }

    void HandleApiException(Exception exception, ILogger logger, int errorCode)
    {
        bool isErrorCodeRelatedToHttpStatus = Enum.IsDefined(typeof(HttpStatusCode), errorCode);
        HttpStatusCode? statusCode = isErrorCodeRelatedToHttpStatus ? (HttpStatusCode?)errorCode : null;
        string? troubleShootingLink = null;

        if (statusCode is not null)
        {
            HttpErrorTroubleshootingLinks.TryGetValue(statusCode.Value, out troubleShootingLink);
        }

        var fullExceptionMessage = troubleShootingLink is null
            ? exception.Message
            : string.Join(Environment.NewLine, exception.Message, TroubleshootingHelp, troubleShootingLink);

        logger.LogError(fullExceptionMessage);
    }
}
