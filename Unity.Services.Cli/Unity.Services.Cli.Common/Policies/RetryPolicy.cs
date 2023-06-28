using System.Net;
using Polly;
using RestSharp;

namespace Unity.Services.Cli.Common.Policies;

public static class RetryPolicy
{
    const int k_NbRetries = 3;
    internal const double MaxJitter = 0.5;
    const double k_ConstantBackoffDelay = 3;
    internal const double MaxRetryDelay = 15;

    /// <summary>
    /// For the Remote Service API HTTP Request policy, this is a whitelist of the statuses to retry for
    /// </summary>
    internal static readonly HttpStatusCode[] RetryHttpStatuses =
    {
        HttpStatusCode.RequestTimeout,
        HttpStatusCode.TooManyRequests,
        HttpStatusCode.InternalServerError,
        HttpStatusCode.BadGateway,
        HttpStatusCode.ServiceUnavailable,
        HttpStatusCode.GatewayTimeout
    };

    /// <summary>
    /// Defines a synchronous retry policy for Remote Service API HTTP Requests.
    /// This is meant to be used as template by services, but can be used for other HTTP request purposes.
    /// </summary>
    /// <param name="nbRetries">The number of retries to attempt</param>
    /// <param name="withJitter">Whether to add jitter or not to the backoff</param>
    /// <returns>A synchronous HTTP retry policy</returns>
    public static Policy<RestResponse> GetHttpRetryPolicy(int nbRetries = k_NbRetries, bool withJitter = true)
    {
        // Trick to get Policy to bubble up the latest exception thrown by the retry policy defined below
        var fallback = Policy
            .HandleResult<RestResponse>(_ => false)
            // Never happens
            .Fallback(_ => null!);

        var retry = Policy
            .HandleResult<RestResponse>(ShouldRetryHttpRequest)
            .WaitAndRetry(
                nbRetries,
                (retryCount, result, _) => GetRetryTimeSpan(retryCount, result, withJitter));

        return fallback.Wrap(retry);
    }

    /// <summary>
    /// Defines an async retry policy for Remote Service API HTTP Requests.
    /// This is meant to be used as template by services, but can be used for other HTTP request purposes.
    /// </summary>
    /// <param name="nbRetries">The number of retries to attempt</param>
    /// <param name="withJitter">Whether to add jitter or not to the backoff</param>
    /// <returns>An asynchronous HTTP retry policy</returns>
    public static AsyncPolicy<RestResponse> GetAsyncHttpRetryPolicy(int nbRetries = k_NbRetries, bool withJitter = true)
    {
        // Trick to get Policy to bubble up the latest exception thrown by the retry policy defined below
        var fallback = Policy
            .HandleResult<RestResponse>(_ => false)
            // Never happens
            .FallbackAsync(_ => null);

        var retry = Policy
            .HandleResult<RestResponse>(ShouldRetryHttpRequest)
            .WaitAndRetryAsync(
                nbRetries,
                (retryCount, result, _) => GetRetryTimeSpan(retryCount, result, withJitter),
                (_, _, _, _) => Task.CompletedTask);

        return fallback.WrapAsync(retry);
    }

    /// <summary>
    /// Get the amount of time to wait before making another retry request.
    /// </summary>
    /// <param name="retryCount">The current retry counter</param>
    /// <param name="result">Http response result delegate</param>
    /// <param name="withJitter">Whether to add jitter or not to the backoff</param>
    /// <returns>A TimeSpan containing the timing information for the next retry run</returns>
    static TimeSpan GetRetryTimeSpan(int retryCount, DelegateResult<RestResponse> result, bool withJitter)
    {
        TimeSpan timeSpan = GetTimeSpanFromRetryAfterHeader(result.Result) ??
                            GetExponentialBackoffTimeSpan(retryCount, withJitter);

        if (timeSpan.TotalSeconds > MaxRetryDelay)
        {
            return TimeSpan.FromSeconds(MaxRetryDelay);
        }

        return timeSpan;
    }

    static bool ShouldRetryHttpRequest(RestResponse response)
        => RetryHttpStatuses.Contains(response.StatusCode);

    /// <summary>
    /// From an http RestResponse, attempt to find the "Retry-After" header and get its value to determine when
    /// the next retry should be executed
    /// </summary>
    /// <param name="httpResponse">Http response to parse</param>
    /// <returns>A TimeSpan containing the timing information for the next retry run</returns>
    internal static TimeSpan? GetTimeSpanFromRetryAfterHeader(RestResponse httpResponse)
    {
        HeaderParameter? param = httpResponse.Headers?
            .ToList()
            .Find(headerParameter => headerParameter.Name == "Retry-After");

        string? retryAfterValue = param?.Value?.ToString();

        if (retryAfterValue == null)
            return null;

        // At this point, the Retry-After header can be in two formats.
        // Format 1 being an int with the number of seconds to wait, ex: 120
        if (int.TryParse(retryAfterValue, out int seconds))
        {
            return TimeSpan.FromSeconds(seconds);
        }

        // Format 2 being a date in html ugc format, ex: Wed, 21 Oct 2023 07:24:00 GMT
        if (DateTime.TryParse(retryAfterValue, out DateTime date))
        {
            return date - DateTime.Now;
        }

        return null;
    }

    /// <summary>
    /// Generates a TimeSpan depending on the current retry count, following an exponential backoff policy.
    /// </summary>
    /// <param name="retryAttempt">A count on the number of retries</param>
    /// <param name="withJitter">Whether to add jitter or not to the backoff</param>
    /// <returns>A TimeSpan containing the timing information for the next retry run</returns>
    public static TimeSpan GetExponentialBackoffTimeSpan(
        int retryAttempt = k_NbRetries,
        bool withJitter = false)
        => TimeSpan.FromSeconds(Math.Pow(retryAttempt, 2) + (withJitter ? GenerateJitter() : 0));

    /// <summary>
    /// Generates a TimeSpan depending on the current retry count, following a constant backoff policy.
    /// </summary>
    /// <param name="backoffDelay">The delay in seconds between each try</param>
    /// <param name="withJitter">Whether to add jitter or not to the backoff</param>
    /// <returns>A TimeSpan containing the timing information for the next retry run</returns>
    public static TimeSpan GetConstantBackoffTimeSpan(
        double backoffDelay = k_ConstantBackoffDelay,
        bool withJitter = false)
        => TimeSpan.FromSeconds(backoffDelay + (withJitter ? GenerateJitter() : 0));

    /// <summary>
    /// Generates a random number between 0 and maxJitter.
    /// This number is used to help balance the load of requests made to the servers that receive the request and to
    /// mitigate synchronized behaviour.
    /// </summary>
    /// <returns>Random number between 0 and maxJitter</returns>
    internal static double GenerateJitter()
    {
        Random random = new();
        double jitterMultiplier = random.NextDouble();
        return jitterMultiplier * MaxJitter;
    }
}
