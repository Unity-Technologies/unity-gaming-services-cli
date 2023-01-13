using System.Threading;
using System.Threading.Tasks;
using Moq;
using Unity.Services.Gateway.IdentityApiV1.Generated.Api;
using Unity.Services.Gateway.IdentityApiV1.Generated.Model;

namespace Unity.Services.Cli.Environment.UnitTest.Mock;

class IdentityApiV1AsyncMock
{
    public Mock<IEnvironmentApi> DefaultApiAsyncObject { get; } = new();
    public ListEnvironmentsResponse Response { get; } = new();
    public EnvironmentResponse AddedEnvironment { get; } = new();

    public bool IsUnityGetEnvironmentsAsyncCalled { get; private set; }
    public bool IsUnityDeleteEnvironmentAsyncCalled { get; private set; }

    public bool IsUnityAddEnvironmentAsyncCalled { get; private set; }

    public void SetUpIdentityApiV1Async()
    {
        IsUnityGetEnvironmentsAsyncCalled = false;
        IsUnityDeleteEnvironmentAsyncCalled = false;
        IsUnityAddEnvironmentAsyncCalled = false;

        DefaultApiAsyncObject.Setup(a =>
                a.GetEnvironmentsAsync(It.IsAny<string>(), 0, CancellationToken.None))
            .Returns(Task.FromResult(Response)).Callback(() => IsUnityGetEnvironmentsAsyncCalled = true);

        DefaultApiAsyncObject.Setup(a =>
                a.DeleteEnvironmentAsync(It.IsAny<string>(), It.IsAny<string>(), 0, CancellationToken.None))
            .Returns(Task.FromResult(new object())).Callback(() => IsUnityDeleteEnvironmentAsyncCalled = true);

        DefaultApiAsyncObject.Setup(a =>
                a.CreateEnvironmentAsync(It.IsAny<string>(), It.IsAny<EnvironmentRequestBody>(), 0, CancellationToken.None))
            .Returns(Task.FromResult(AddedEnvironment)).Callback(() => IsUnityAddEnvironmentAsyncCalled = true);

        DefaultApiAsyncObject.Setup(a => a.Configuration)
            .Returns(new Gateway.IdentityApiV1.Generated.Client.Configuration());
    }
}
