using Moq;
using NUnit.Framework;
using Spectre.Console;
using Unity.Services.Cli.Common.Console;

namespace Unity.Services.Cli.Common.UnitTest.Console;

[TestFixture]
class ProgressBarTests
{
    readonly Mock<IAnsiConsole> m_MockAnsiConsole = new();
    MockProgressExclusiveMode? m_ExclusiveMode;
    ProgressBar? m_ProgressBar;
    bool m_ProgressCallbackIsCalled;

    [SetUp]
    public void Setup()
    {
        m_ProgressCallbackIsCalled = false;
        m_ExclusiveMode = new (EmptyProgressCallbackMethod);
        m_MockAnsiConsole.SetupGet(a => a.ExclusivityMode).Returns(m_ExclusiveMode);
    }

    [Test]
    public async Task ProgressBarStartProgressCallsCallbackWhenConsoleIsSet()
    {
        m_ProgressBar = new(m_MockAnsiConsole.Object);
        await m_ProgressBar.StartProgressAsync(EmptyProgressCallbackMethod);
        Assert.IsTrue(m_ProgressCallbackIsCalled);
    }

    [Test]
    public async Task ProgressBarStartProgressCallsCallbackWhenConsoleNull()
    {
        m_ProgressBar = new(null);
        await m_ProgressBar.StartProgressAsync(EmptyProgressCallbackMethod);
        Assert.IsTrue(m_ProgressCallbackIsCalled);
    }

    Task<bool> EmptyProgressCallbackMethod(ProgressContext? context)
    {
        m_ProgressCallbackIsCalled = true;
        return Task.FromResult(m_ProgressCallbackIsCalled);
    }
}
