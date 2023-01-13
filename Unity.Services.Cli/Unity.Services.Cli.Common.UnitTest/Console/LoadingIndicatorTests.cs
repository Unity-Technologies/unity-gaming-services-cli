using Moq;
using NUnit.Framework;
using Spectre.Console;
using Unity.Services.Cli.Common.Console;

namespace Unity.Services.Cli.Common.UnitTest.Console;

[TestFixture]
class LoadingIndicatorTests
{
    readonly Mock<IAnsiConsole> m_MockAnsiConsole = new();
    MockLoadingExclusiveMode? m_ExclusiveMode;
    LoadingIndicator? m_LoadingIndicator;
    bool m_StatusCallbackIsCalled;

    [SetUp]
    public void Setup()
    {
        m_StatusCallbackIsCalled = false;
        m_ExclusiveMode = new (EmptyLoadingCallbackMethod);
        m_MockAnsiConsole.SetupGet(a => a.ExclusivityMode).Returns(m_ExclusiveMode);
    }

    [Test]
    public async Task LoadingIndicatorStartLoadingCallsCallbackWhenConsoleIsSet()
    {
        m_LoadingIndicator = new(m_MockAnsiConsole.Object);
        await m_LoadingIndicator.StartLoadingAsync("task", EmptyLoadingCallbackMethod);
        Assert.IsTrue(m_StatusCallbackIsCalled);
    }

    [Test]
    public async Task ProgressBarStartProgressCallsCallbackWhenConsoleNull()
    {
        m_LoadingIndicator = new(null);
        await m_LoadingIndicator.StartLoadingAsync("task", EmptyLoadingCallbackMethod);
        Assert.IsTrue(m_StatusCallbackIsCalled);
    }

    Task<bool> EmptyLoadingCallbackMethod(StatusContext? context)
    {
        m_StatusCallbackIsCalled = true;
        return Task.FromResult(m_StatusCallbackIsCalled);
    }
}
