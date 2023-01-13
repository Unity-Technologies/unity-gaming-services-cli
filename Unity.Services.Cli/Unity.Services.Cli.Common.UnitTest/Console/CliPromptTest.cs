using Moq;
using NUnit.Framework;
using Spectre.Console;
using Unity.Services.Cli.Common.Console;

namespace Unity.Services.Cli.Common.UnitTest.Console;

[TestFixture]
public class CliPromptTest
{
    readonly Mock<IAnsiConsole> m_MockAnsiConsole = new();
    readonly Mock<IPrompt<string>> m_MockPrompt = new();

    [SetUp]
    public void SetUp()
    {
        m_MockAnsiConsole.Reset();
        m_MockPrompt.Reset();
    }

    [Test]
    public async Task PromptAsyncSucceed()
    {
        const string expectedString = "foo";
        var prompt = new CliPrompt(m_MockAnsiConsole.Object);
        m_MockPrompt.Setup(p => p.ShowAsync(m_MockAnsiConsole.Object, CancellationToken.None))
            .ReturnsAsync(expectedString);

        var actualString = await prompt.PromptAsync(m_MockPrompt.Object, CancellationToken.None);

        Assert.AreEqual(expectedString, actualString);
    }
}
