using System.Net;
using Moq;
using Unity.Services.Gateway.ObservabilityApiV1.Generated.Api;
using Unity.Services.Gateway.ObservabilityApiV1.Generated.Client;
using Unity.Services.Gateway.ObservabilityApiV1.Generated.Model;

namespace Unity.Services.Cli.Observability.UnitTest.Mock;

class LogsApiAsyncMock
{
    public Mock<ILogsApiAsync> DefaultApiAsyncObject = new();

    public ApiResponse<LogsResponse> GetLogsResponse { get; set; } =
        new(statusCode: HttpStatusCode.OK, data: new LogsResponse(0, 100, 0, new List<LogRecord>()));

    public void SetUp()
    {
        DefaultApiAsyncObject.Reset();
        DefaultApiAsyncObject.Setup(a => a.Configuration)
            .Returns(new Gateway.ObservabilityApiV1.Generated.Client.Configuration());

        DefaultApiAsyncObject.Setup(
                a => a.GetLogsWithHttpInfoAsync(
                    It.IsAny<string>(),
                    It.IsAny<Guid>(),
                    It.IsAny<int?>(),
                    It.IsAny<int?>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<int>(),
                    CancellationToken.None))
            .ReturnsAsync(GetLogsResponse);
    }
}
