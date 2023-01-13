using Spectre.Console;

namespace Unity.Services.Cli.Common.UnitTest.Console;

public class MockProgressExclusiveMode : IExclusivityMode
{
    readonly Func<ProgressContext?, Task> m_Action;

    public MockProgressExclusiveMode(Func<ProgressContext?, Task> action)
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
