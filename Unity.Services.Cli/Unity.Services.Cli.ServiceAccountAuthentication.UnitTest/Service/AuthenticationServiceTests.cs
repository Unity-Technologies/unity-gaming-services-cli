using Moq;
using NUnit.Framework;
using Unity.Services.Cli.Common.Persister;
using Unity.Services.Cli.Common.SystemEnvironment;
using Unity.Services.Cli.ServiceAccountAuthentication;
using Unity.Services.Cli.ServiceAccountAuthentication.Exceptions;

namespace Unity.Services.Cli.Authentication.UnitTest;

[TestFixture]
class AuthenticationServiceTests
{
    [Test]
    public async Task GetAccessTokenAsyncReturnsPersistedTokenIfAny()
    {
        Mock<ISystemEnvironmentProvider> mockEnvironmentProvider = new();
        const string expectedToken = "test token";
        var persister = FakePersister(FakeLoad);
        var service = new AuthenticationService(persister, mockEnvironmentProvider.Object);

        var token = await service.GetAccessTokenAsync();

        Assert.AreEqual(expectedToken, token);

        Task<string?> FakeLoad(CancellationToken _) => Task.FromResult<string?>(expectedToken);
    }

    [Test]
    public void GetAccessTokenAsyncThrowsIfNothingIsPersisted()
    {
        Mock<ISystemEnvironmentProvider> mockEnvironmentProvider = new();
        var persister = FakePersister(FakeLoad);
        var service = new AuthenticationService(persister, mockEnvironmentProvider.Object);

        Assert.ThrowsAsync<MissingAccessTokenException>(() => service.GetAccessTokenAsync());

        Task<string?> FakeLoad(CancellationToken _) => Task.FromResult<string?>(null);
    }

    static IPersister<string> FakePersister(Func<CancellationToken, Task<string?>> fakeLoad)
    {
        var mock = new Mock<IPersister<string>>();
        mock.Setup(x => x.LoadAsync(It.IsAny<CancellationToken>()))
            .Returns(fakeLoad);
        return mock.Object;
    }
}
