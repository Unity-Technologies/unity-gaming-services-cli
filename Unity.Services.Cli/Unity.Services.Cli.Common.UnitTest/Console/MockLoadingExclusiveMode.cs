using Spectre.Console;

namespace Unity.Services.Cli.Common.UnitTest.Console;

public class MockLoadingExclusiveMode : IExclusivityMode
{
    readonly Func<StatusContext?, Task> m_Action;

    public MockLoadingExclusiveMode(Func<StatusContext?, Task> action)
    {
        m_Action = action;
    }

    public T Run<T>(Func<T> func)
    {
        throw new NotImplementedException();
    }

    public async Task<T> RunAsync<T>(Func<Task<T>> func)
    {
        await m_Action(null).ConfigureAwait(false);

        return await Task.FromResult<T>(default!);
    }
}
