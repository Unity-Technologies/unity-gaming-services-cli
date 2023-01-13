using NUnit.Framework;
using Unity.Services.Cli.Common.Handlers;
using Unity.Services.Cli.Common.Input;
using Unity.Services.Cli.TestUtils;

namespace Unity.Services.Cli.Common.UnitTest.Handlers;

[TestFixture]
class SetHandlerTests
{
    [Test]
    public async Task SetAsyncCallsConfig()
    {
        MockHelper mockHelper = new();
        var input = new ConfigurationInput
        {
            Key = Models.Keys.ConfigKeys.ProjectId,
            Value = "fake-project-id"
        };

        await SetHandler.SetAsync(input, mockHelper.MockConfiguration.Object,
            mockHelper.MockLogger.Object, CancellationToken.None);

        mockHelper.MockConfiguration.Verify(c => c.SetConfigArgumentsAsync(input.Key, input.Value, CancellationToken.None));

    }
}
