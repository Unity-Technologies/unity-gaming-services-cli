using Spectre.Console;
using Unity.Services.Cli.Common.Console;

namespace Unity.Services.Cli.GameServerHosting.UnitTest.Mocks;

public class LoadingIndicatorMock: ILoadingIndicator
{
    public Task StartLoadingAsync(string description, Func<StatusContext?, Task> callback)
    {
        return callback(null);
    }
}
