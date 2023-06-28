using System.Net;
using NUnit.Framework;
using RestSharp;
using Unity.Services.Cli.Common.Policies;

namespace Unity.Services.Cli.Common.UnitTest.Policies;

[TestFixture]
public class RetryPolicyTests
{
    const string k_BlankUri = "https://blank.org";
    const string k_RetryAfterHeaderName = "Retry-After";
    static readonly HttpStatusCode[] k_HttpStatusCodeCases = RetryPolicy.RetryHttpStatuses;
    TestHttpMessageHandler? m_MessageHandler;

    [SetUp]
    public void SetUp()
    {
        m_MessageHandler = new TestHttpMessageHandler();
    }

    class TestHttpMessageHandler : HttpMessageHandler
    {
        readonly Queue<HttpResponseMessage> m_ResponseMessages = new();
        internal int RequestCount { get; private set; }

        internal void EnqueueHttpResponseMessage(HttpResponseMessage responseMessage)
        {
            m_ResponseMessages.Enqueue(responseMessage);
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            RequestCount++;
            return Task.FromResult(m_ResponseMessages.Dequeue());
        }

        protected override HttpResponseMessage Send(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            RequestCount++;
            return m_ResponseMessages.Dequeue();
        }
    }

    [Test]
    public void AsyncHttpRetryPolicyBubblesUpException()
    {
        var policy = RetryPolicy.GetAsyncHttpRetryPolicy(1, false);

        Assert.IsNotNull(policy);

        Assert.ThrowsAsync<Exception>(async () =>
        {
            await policy.ExecuteAsync(() => throw new Exception());
        });
    }

    [Test]
    public void HttpRetryPolicyBubblesUpException()
    {
        var policy = RetryPolicy.GetHttpRetryPolicy(1, false);

        Assert.IsNotNull(policy);

        Assert.Throws<Exception>(() =>
        {
            policy.Execute(() => throw new Exception());
        });
    }

    [TestCaseSource(nameof(k_HttpStatusCodeCases))]
    public async Task AsyncHttpRetryPolicyRetriesRequestOnErrorStatusCodes(HttpStatusCode statusCode)
    {
        const int nbRetries = 1;

        // Queue http responses that give off the desired status code
        for (int i = 0; i < nbRetries + 1; i++)
        {
            m_MessageHandler!.EnqueueHttpResponseMessage(new HttpResponseMessage(statusCode));
        }

        var httpClient = new HttpClient(m_MessageHandler!);
        var policy = RetryPolicy.GetAsyncHttpRetryPolicy(nbRetries, false);

        Assert.IsNotNull(policy);

        // Use the policy to retry the mocked http requests
        await policy.ExecuteAsync(async () => await MakeMockAsyncHttpRequest(httpClient));

        Assert.AreEqual(nbRetries + 1, m_MessageHandler!.RequestCount);
    }

    [TestCaseSource(nameof(k_HttpStatusCodeCases))]
    public void HttpRetryPolicyRetriesRequestOnErrorStatusCodes(HttpStatusCode statusCode)
    {
        const int nbRetries = 1;

        // Queue http responses that give off the desired status code
        for (int i = 0; i < nbRetries + 1; i++)
        {
            m_MessageHandler!.EnqueueHttpResponseMessage(new HttpResponseMessage(statusCode));
        }

        var httpClient = new HttpClient(m_MessageHandler!);
        var policy = RetryPolicy.GetHttpRetryPolicy(nbRetries, false);

        Assert.IsNotNull(policy);

        // Use the policy to retry the mocked http requests
        policy.Execute(() => MakeMockHttpRequest(httpClient));

        Assert.AreEqual(nbRetries + 1, m_MessageHandler!.RequestCount);
    }

    [Test]
    public async Task AsyncHttpRetryPolicyStopsRetryingAfterSuccess()
    {
        const int nbRetries = 5;

        m_MessageHandler!.EnqueueHttpResponseMessage(new HttpResponseMessage(HttpStatusCode.TooManyRequests));
        m_MessageHandler!.EnqueueHttpResponseMessage(new HttpResponseMessage(HttpStatusCode.OK));

        var httpClient = new HttpClient(m_MessageHandler!);
        var policy = RetryPolicy.GetAsyncHttpRetryPolicy(nbRetries, false);

        Assert.IsNotNull(policy);

        // Use the policy to retry the mocked http requests
        await policy.ExecuteAsync(async () => await MakeMockAsyncHttpRequest(httpClient));

        Assert.AreEqual(2, m_MessageHandler!.RequestCount);
    }

    [Test]
    public void HttpRetryPolicyStopsRetryingAfterSuccess()
    {
        const int nbRetries = 5;

        m_MessageHandler!.EnqueueHttpResponseMessage(new HttpResponseMessage(HttpStatusCode.TooManyRequests));
        m_MessageHandler!.EnqueueHttpResponseMessage(new HttpResponseMessage(HttpStatusCode.OK));

        var httpClient = new HttpClient(m_MessageHandler!);
        var policy = RetryPolicy.GetHttpRetryPolicy(nbRetries, false);

        Assert.IsNotNull(policy);

        // Use the policy to retry the mocked http requests
        policy.Execute(() => MakeMockHttpRequest(httpClient));

        Assert.AreEqual(2, m_MessageHandler!.RequestCount);
    }

    static RestResponse MakeMockHttpRequest(HttpClient httpClient)
    {
        var responseMessage = httpClient.Send(new HttpRequestMessage(HttpMethod.Get, k_BlankUri));

        return CreateRestResponseFromHttpResponseMessage(responseMessage);
    }

    static async Task<RestResponse> MakeMockAsyncHttpRequest(HttpClient httpClient)
    {
        var responseMessage = await httpClient.GetAsync(k_BlankUri);

        return CreateRestResponseFromHttpResponseMessage(responseMessage);
    }

    static RestResponse CreateRestResponseFromHttpResponseMessage(HttpResponseMessage responseMessage)
    {
        RestResponse restResponse = new RestResponse
        {
            StatusCode = responseMessage.StatusCode,
            ResponseStatus = ResponseStatus.Completed,
        };

        var retryAfterDelta = responseMessage.Headers.RetryAfter?.Delta;
        if (retryAfterDelta != null)
        {
            restResponse.Headers = new[]
            {
                new HeaderParameter("Retry-After", retryAfterDelta.Value.TotalSeconds.ToString())
            };
        }

        return restResponse;
    }

    [TestCase("2", 2)]
    [TestCase("10", 10)]
    public void GetTimeSpanFromRetryAfterHeaderInSecondsReturnsCorrectTimeSpan(string retryAfterValue, double expected)
    {
        RestResponse response = new RestResponse();
        HeaderParameter retryAfterHeader = new HeaderParameter(k_RetryAfterHeaderName, retryAfterValue);

        response.Headers = new[]
        {
            retryAfterHeader
        };

        TimeSpan? timeSpan = RetryPolicy.GetTimeSpanFromRetryAfterHeader(response);

        Assert.NotNull(timeSpan);
        Assert.AreEqual(expected, timeSpan!.Value.TotalSeconds);
    }

    [TestCase("Sat, 1 Mar 2025 08:00:00 GMT")]
    [TestCase("Tue, 1 Jan 2030 08:00:00 GMT")]
    public void GetTimeSpanFromRetryAfterHeaderValidDateReturnsValidTimeSpan(string retryAfterValue)
    {
        RestResponse response = new RestResponse();
        HeaderParameter retryAfterHeader = new HeaderParameter(k_RetryAfterHeaderName, retryAfterValue);

        response.Headers = new[]
        {
            retryAfterHeader
        };

        TimeSpan? timeSpan = RetryPolicy.GetTimeSpanFromRetryAfterHeader(response);

        Assert.NotNull(timeSpan);
    }

    [TestCase("Sat, 2 Mar 2025 08:00:00 GMT")]
    public void GetTimeSpanFromRetryAfterHeaderInvalidDateReturnsNullTimeSpan(string retryAfterValue)
    {
        RestResponse response = new RestResponse();
        HeaderParameter retryAfterHeader = new HeaderParameter(k_RetryAfterHeaderName, retryAfterValue);

        response.Headers = new[]
        {
            retryAfterHeader
        };

        TimeSpan? timeSpan = RetryPolicy.GetTimeSpanFromRetryAfterHeader(response);

        Assert.IsNull(timeSpan);
    }

    [Test]
    public void GetTimeSpanFromRetryAfterHeaderWithRequestWithoutHeaderReturnsNull()
    {
        RestResponse response = new RestResponse();
        TimeSpan? timeSpan = RetryPolicy.GetTimeSpanFromRetryAfterHeader(response);
        Assert.IsNull(timeSpan);
    }

    [TestCase(1, 1)]
    [TestCase(2, 4)]
    [TestCase(3, 9)]
    public void GetExponentialBackoffTimeSpanWithoutJitterReturnsCorrectTimeSpan(int retryCount, int expectedSeconds)
    {
        TimeSpan expectedTimeSpan = TimeSpan.FromSeconds(expectedSeconds);
        Assert.AreEqual(expectedTimeSpan, RetryPolicy.GetExponentialBackoffTimeSpan(retryCount));
    }

    [TestCase(1)]
    [TestCase(5)]
    public void GetConstantBackoffTimeSpanWithoutJitterReturnsCorrectTimeSpan(int seconds)
    {
        TimeSpan expectedTimeSpan = TimeSpan.FromSeconds(seconds);
        Assert.AreEqual(expectedTimeSpan, RetryPolicy.GetConstantBackoffTimeSpan(seconds));
    }

    [TestCase(2, 4)]
    [Repeat(20)]
    public void GetExponentialBackoffTimeSpanWithJitterReturnsCorrectTimeSpan(int retryCount, int expectedSeconds)
    {
        TimeSpan timeSpan = RetryPolicy.GetExponentialBackoffTimeSpan(retryCount, true);

        Assert.That(timeSpan.TotalSeconds <= expectedSeconds + RetryPolicy.MaxJitter &&
                    timeSpan.TotalSeconds >= expectedSeconds);
    }

    [TestCase(3)]
    [Repeat(20)]
    public void GetConstantBackoffTimeSpanWithJitterReturnsCorrectTimeSpan(int seconds)
    {
        TimeSpan timeSpan = RetryPolicy.GetConstantBackoffTimeSpan(seconds, true);

        Assert.That(timeSpan.TotalSeconds <= seconds + RetryPolicy.MaxJitter &&
                    timeSpan.TotalSeconds >= seconds);
    }

    [Test]
    [Repeat(20)]
    public void GenerateJitterGeneratesRandomNumberInCorrectRange()
    {
        double jitter = RetryPolicy.GenerateJitter();
        Assert.That(jitter >= 0 && jitter <= RetryPolicy.MaxJitter);
    }
}
