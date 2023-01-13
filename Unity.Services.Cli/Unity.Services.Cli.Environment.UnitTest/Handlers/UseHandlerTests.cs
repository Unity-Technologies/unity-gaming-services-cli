using System.Threading;
using NUnit.Framework;
using Unity.Services.Cli.Environment.Handlers;
using Unity.Services.Cli.Environment.Input;
using Unity.Services.Cli.TestUtils;
using Models = Unity.Services.Cli.Common.Models;

namespace Unity.Services.Cli.Environment.UnitTest.Handlers;

[TestFixture]
class UseHandlerTests
{
    readonly MockHelper m_MockHelper = new();

    [SetUp]
    public void SetUp()
    {
        m_MockHelper.ClearInvocations();
    }

    [Test]
    public void UseCommandAsync_CallsConfigurationService()
    {
        var input = new EnvironmentInput
        {
            EnvironmentName = "staging"
        };

        Assert.DoesNotThrowAsync(() =>
            UseHandler.UseAsync(input, m_MockHelper.MockConfiguration.Object, m_MockHelper.MockLogger.Object, CancellationToken.None));
        m_MockHelper.MockConfiguration.Verify(s =>
            s.SetConfigArgumentsAsync(Models.Keys.ConfigKeys.EnvironmentName, input.EnvironmentName, CancellationToken.None));
    }
}
