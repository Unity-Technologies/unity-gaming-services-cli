using Microsoft.Extensions.Hosting;
using Moq;
using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NUnit.Framework;

namespace Unity.Services.Cli.TestUtils;

public static class TestsHelper
{
    public static void AssertContainsCommand(Command command, string commandName, out Command match)
    {
        match = (Command)command.AsEnumerable().First(a => a.Name == commandName);
        Assert.IsNotNull(match);
    }

    public static IHostBuilder CreateAndSetupMockHostBuilder(List<ServiceDescriptor> services)
    {
        var serviceCollection = CreateAndSetupMockServiceCollection(services);
        var mock = new Mock<IHostBuilder>();
        mock.Setup(x => x.ConfigureServices(It.IsAny<Action<HostBuilderContext, IServiceCollection>>()))
            .Callback(FakeConfigureServices)
            .Returns(mock.Object);
        return mock.Object;

        void FakeConfigureServices(Action<HostBuilderContext, IServiceCollection> callback)
        {
            callback.Invoke(new HostBuilderContext(new Dictionary<object, object>()), serviceCollection);
        }
    }

    static IServiceCollection CreateAndSetupMockServiceCollection(IList<ServiceDescriptor> services)
    {
        var mock = new Mock<IServiceCollection>();
        mock.Setup(x => x.Add(It.IsAny<ServiceDescriptor>()))
            .Callback<ServiceDescriptor>(services.Add);

        // Count, GetEnumerator(), and CopyTo(...) are required to support ServiceProvider construction.
        mock.Setup(x => x.Count)
            .Returns(services.Count);
        mock.Setup(x => x.GetEnumerator())
            .Returns(services.GetEnumerator);
        mock.Setup(x => x.CopyTo(It.IsAny<ServiceDescriptor[]>(), It.IsAny<int>()))
            .Callback<ServiceDescriptor[], int>(services.CopyTo);
        return mock.Object;
    }

    /// <summary>
    /// Verify if a mock ILogger was called, with or without an expected LogLevel, EventId and number of calls.
    /// </summary>
    /// <param name="mockLogger">The mocked ILogger to verify</param>
    /// <param name="expectedLogLevel">The expected LogLevel (Leave out to ignore the LogLevel in verification)</param>
    /// <param name="expectedEventId">The expected EventId (Leave out to ignore the EventId in verification)</param>
    /// <param name="expectedTimes">The expected number of calls to the logger (Leave out to verify once)</param>
    /// <param name="message">The string passed to the logger (Verify will check if the log contains the given message)</param>
    public static void VerifyLoggerWasCalled(Mock<ILogger> mockLogger, LogLevel? expectedLogLevel = null,
        EventId? expectedEventId = null, Func<Times>? expectedTimes = null, string? message = null)
    {
        expectedTimes ??= Times.Once;

        mockLogger.Verify(x => x.Log(
            It.Is<LogLevel>(level => VerifyLoggerLogLevel(level, expectedLogLevel)),
            It.Is<EventId>(id => VerifyLoggerEventId(id, expectedEventId)),
            It.Is<It.IsAnyType>((v, t) => message == null || VerifyLoggerMessage(v, message)),
            It.IsAny<Exception>(),
            It.Is<Func<It.IsAnyType, Exception?, string>>((o, t) => true)), expectedTimes);
    }

    public static Mock<IHost> CreateAndSetupMockedHost(out Mock<IServiceProvider> mockedProvider)
    {
        mockedProvider = new Mock<IServiceProvider>();
        var mockedHost = new Mock<IHost>();
        mockedHost.Setup(x => x.Services)
            .Returns(mockedProvider.Object);
        return mockedHost;
    }

    public static Mock<TService> SetupGetMockedService<TService>(Mock<IServiceProvider> mockedProvider)
        where TService : class
    {
        var mockedService = new Mock<TService>();
        mockedProvider.Setup(x => x.GetService(typeof(TService)))
            .Returns(mockedService.Object);
        return mockedService;
    }

    static bool VerifyLoggerLogLevel(LogLevel actualLevel, LogLevel? expectedLevel)
    {
        return expectedLevel == null || actualLevel.Equals(expectedLevel);
    }

    static bool VerifyLoggerEventId(EventId actualEventId, EventId? expectedEventId)
    {
        return expectedEventId == null ||
            actualEventId.Id.Equals(expectedEventId.Value.Id) &&
            string.Equals(actualEventId.Name, expectedEventId.Value.Name);
    }

    static bool VerifyLoggerMessage(object v, string message)
    {
        try
        {
            return v.ToString()!.Contains(message);
        }
        catch
        {
            return false;
        }
    }

    public static void AssertHasServiceSingleton<TService, TImplementation>(IEnumerable<ServiceDescriptor> services)
    {
        var registeredImplementationsForService = services.GroupBy(x => x.ServiceType)
            .First(x => x.Key == typeof(TService));
        Assert.AreEqual(1, registeredImplementationsForService.Count());
        Assert.IsInstanceOf<TImplementation>(registeredImplementationsForService.First().ImplementationInstance);
    }

    public static void AssertHasServiceType<TService>(IEnumerable<ServiceDescriptor> services)
    {
        var registeredTypesForService = services.GroupBy(x => x.ServiceType)
            .First(x => x.Key == typeof(TService));
        Assert.AreEqual(1, registeredTypesForService.Count());
    }
}
