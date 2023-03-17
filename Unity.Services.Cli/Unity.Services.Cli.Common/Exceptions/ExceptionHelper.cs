using System.CommandLine.Invocation;
using System.Net;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using Unity.Services.Cli.Common.Telemetry;
using IdentityApiException = Unity.Services.Gateway.IdentityApiV1.Generated.Client.ApiException;
using CloudCodeApiException = Unity.Services.Gateway.CloudCodeApiV1.Generated.Client.ApiException;
using EconomyApiException = Unity.Services.Gateway.EconomyApiV2.Generated.Client.ApiException;
using LobbyApiException = Unity.Services.MpsLobby.LobbyApiV1.Generated.Client.ApiException;
using LeaderboardApiException = Unity.Services.Gateway.LeaderboardApiV1.Generated.Client.ApiException;
using PlayerAdminApiException = Unity.Services.Gateway.PlayerAdminApiV2.Generated.Client.ApiException;
using PlayerAuthException = Unity.Services.Gateway.PlayerAuthApiV1.Generated.Client.ApiException;

namespace Unity.Services.Cli.Common.Exceptions;

public class ExceptionHelper
{
    IDiagnostics Diagnostics { get; }
    readonly IAnsiConsole m_AnsiConsole;
    internal const string TroubleshootingHelp = "For help troubleshooting this error, visit this page in your browser:";
    internal readonly IReadOnlyDictionary<HttpStatusCode, string> HttpErrorTroubleshootingLinks = new Dictionary<HttpStatusCode, string>
    {
        [HttpStatusCode.Forbidden] = "https://services.docs.unity.com/guides/ugs-cli/latest/general/troubleshooting/unauthorized-error-403"
    };

    public ExceptionHelper(IDiagnostics diagnostics, IAnsiConsole ansiConsole)
    {
        Diagnostics = diagnostics;
        m_AnsiConsole = ansiConsole;
    }

    public void HandleException(Exception exception, ILogger logger, InvocationContext context)
    {
        var cancellationToken = context.GetCancellationToken();
        if (cancellationToken.IsCancellationRequested)
        {
            context.ExitCode = ExitCode.Cancelled;
            return;
        }

        switch (exception)
        {
            case CliException cliException:
                logger.LogError(cliException.Message);
                context.ExitCode = cliException.ExitCode;
                break;
            case DeploymentFailureException deploymentFailureException:
                // We don't log this exception because the deployment content already
                // has all the information regarding any content failure
                context.ExitCode = deploymentFailureException.ExitCode;
                break;
            case IdentityApiException identityApiException:
                HandleApiException(exception, logger, context, identityApiException.ErrorCode);
                break;
            case CloudCodeApiException cloudCodeApiException:
                HandleApiException(exception, logger, context, cloudCodeApiException.ErrorCode);
                break;
            case EconomyApiException economyApiException:
                HandleApiException(exception, logger, context, economyApiException.ErrorCode);
                break;
            case LobbyApiException lobbyApiException:
                HandleApiException(exception, logger, context, lobbyApiException.ErrorCode);
                break;
            case LeaderboardApiException leaderboardApiException:
                HandleApiException(exception, logger, context, leaderboardApiException.ErrorCode);
                break;
            case PlayerAdminApiException playerAdminApiException:
                HandleApiException(exception, logger, context, playerAdminApiException.ErrorCode);
                break;
            case PlayerAuthException playerAuthApiException:
                HandleApiException(exception, logger, context, playerAuthApiException.ErrorCode);
                break;
            case AggregateException aggregateException:
                HandleAggregateException(aggregateException, logger, context);
                break;
            default:
                ExecuteUnhandledExceptionFlow(exception, context);
                break;
        }
    }

    void ExecuteUnhandledExceptionFlow(Exception exception, InvocationContext context)
    {
        context.ExitCode = ExitCode.UnhandledError;
        m_AnsiConsole.WriteException(exception);
        try
        {
            Diagnostics.SendDiagnostic("cli_unhandled_exception", exception.ToString(), context);
        }
        catch
        {
            // Diagnostics sending failures should be silenced as to not interrupt execution
        }
    }

    void HandleAggregateException(AggregateException aggregateException, ILogger logger, InvocationContext context)
    {
        // Check for CLI Exceptions in the aggregated exceptions
        var cliExceptions =
            aggregateException.InnerExceptions.Where(e => e is CliException);

        // Log any CLI Exception found
        foreach (var e in cliExceptions)
        {
            logger.LogError(e.Message);
        }

        // Sets handled error in case no unhandled error is found
        if (cliExceptions.Any())
        {
            context.ExitCode = ExitCode.HandledError;
        }
        var unhandledExceptions =
            aggregateException.InnerExceptions.Where(e => e is not CliException);

        // Sets default flow in case any exception is unhandled
        if (unhandledExceptions.Any())
        {
            ExecuteUnhandledExceptionFlow(aggregateException, context);
        }
    }

    void HandleApiException(Exception exception, ILogger logger, InvocationContext context, int errorCode)
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
        context.ExitCode = ExitCode.HandledError;
    }
}
