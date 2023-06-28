using NUnit.Framework;

namespace Unity.Services.Cli.Common.UnitTest.Batching;

using Unity.Services.ModuleTemplate.Authoring.Core.Batching;

[TestFixture]
public class BatchingTests
{
    readonly object m_BatchItemInvokeCountLock = new();
    int m_BatchItemInvokeCount;
    const int k_BatchSize = 2;
    const double k_SecondsDelay = 0;
    const int k_IntReturnValue = 1;

    [TearDown]
    public void TearDown()
    {
        m_BatchItemInvokeCount = 0;
    }

    [Test]
    public void ExecuteFuncNoResultBatchAsyncHandlesExceptionsCorrectly()
    {
        List<Func<Task>> delegates = new List<Func<Task>>
        {
            AsyncTaskWithoutReturnValueThrows,
            AsyncTaskWithoutReturnValueThrows,
            AsyncTaskWithoutReturnValueThrows
        };

        var exception = Assert.ThrowsAsync<AggregateException>(
            async () => await Batching.ExecuteInBatchesAsync(
                delegates,
                CancellationToken.None, k_BatchSize, k_SecondsDelay));

        Assert.AreEqual(
            delegates.Count,
            exception?.InnerExceptions.Count);

        async Task AsyncTaskWithoutReturnValueThrows()
        {
            await Task.CompletedTask;
            throw new Exception();
        }
    }

    [Test]
    public void ExecuteFuncResultBatchAsyncHandlesExceptionsCorrectly()
    {
        List<Func<Task<int>>> delegates = new List<Func<Task<int>>>
        {
            AsyncTaskWithReturnValueThrows,
            AsyncTaskWithReturnValueThrows,
            AsyncTaskWithReturnValueThrows
        };

        var exception = Assert.ThrowsAsync<AggregateException>(
            async () => await Batching.ExecuteInBatchesAsync(
                delegates,
                CancellationToken.None, k_BatchSize, k_SecondsDelay));

        Assert.AreEqual(
            delegates.Count,
            exception?.InnerExceptions.Count);

        async Task<int> AsyncTaskWithReturnValueThrows()
        {
            await Task.CompletedTask;
            throw new Exception();
        }
    }

    [Test]
    public async Task ExecuteFuncNoResultBatchesAsyncInvokesAction()
    {
        List<Func<Task>> delegates = new List<Func<Task>>
        {
            AsyncTaskWithoutReturnValue,
            AsyncTaskWithoutReturnValue,
            AsyncTaskWithoutReturnValue
        };

        await Batching.ExecuteInBatchesAsync(delegates,
            CancellationToken.None, k_BatchSize, k_SecondsDelay);

        Assert.AreEqual(delegates.Count, m_BatchItemInvokeCount);

        async Task AsyncTaskWithoutReturnValue()
        {
            lock (m_BatchItemInvokeCountLock)
            {
                m_BatchItemInvokeCount++;
            }
            await Task.CompletedTask;
        }
    }

    [Test]
    public async Task ExecuteFuncResultBatchesAsyncInvokesAction()
    {
        List<Func<Task<int>>> delegates = new List<Func<Task<int>>>
        {
            AsyncTaskWithReturnValue,
            AsyncTaskWithReturnValue,
            AsyncTaskWithReturnValue
        };

        await Batching.ExecuteInBatchesAsync(delegates,
            CancellationToken.None, k_BatchSize, k_SecondsDelay);

        Assert.AreEqual(delegates.Count, m_BatchItemInvokeCount);

        async Task<int> AsyncTaskWithReturnValue()
        {
            lock (m_BatchItemInvokeCountLock)
            {
                m_BatchItemInvokeCount++;
            }
            await Task.CompletedTask;
            return k_IntReturnValue;
        }
    }
}
