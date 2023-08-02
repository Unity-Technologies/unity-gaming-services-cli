using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Unity.Services.Leaderboards.Authoring.Core.Batching
{
    /// <summary>An utility class for executing delegates in batches with a time interval between them</summary>
    /// <warning>Currently only supports async delegates (with or without return values)</warning>
    public static class Batching
    {
        const int k_BatchSize = 10;
        const double k_SecondsDelay = 1;

        const string k_BatchingExceptionMessage =
            "One or more exceptions were thrown during the batching execution. See inner exceptions.";

        public static async Task ExecuteInBatchesAsync(
            IEnumerable<Task> tasks,
            CancellationToken cancellationToken,
            int batchSize = k_BatchSize,
            double secondsDelay = k_SecondsDelay)
        {
            var exceptions = new List<Exception>();
            var iterator = tasks.GetEnumerator();

            while (true)
            {
                var chunk = new List<Task>();
                for (int i = 0; i < batchSize; ++i)
                {
                    if (!iterator.MoveNext())
                        break;
                    chunk.Add(iterator.Current);
                }

                if (chunk.Count == 0)
                    break;

                var innerExceptions = await ExecuteBatchAsync(chunk);
                exceptions.AddRange(innerExceptions);

                if (cancellationToken.IsCancellationRequested)
                    break;

                await Task.Delay(TimeSpan.FromSeconds(secondsDelay), cancellationToken);

                if (cancellationToken.IsCancellationRequested)
                    break;
            }

            iterator.Dispose();

            if (exceptions.Count != 0)
            {
                throw new AggregateException(k_BatchingExceptionMessage, exceptions.ToList());
            }
        }

        static async Task<IReadOnlyList<Exception>> ExecuteBatchAsync(IEnumerable<Task> insideTasks)
        {
            var tasks = new ConcurrentBag<Task>();
            var exceptions = new ConcurrentQueue<Exception>();

            Parallel.ForEach(
                insideTasks,
                async del =>
                {
                    tasks.Add(del);
                    try
                    {
                        await del;
                    }
                    catch (Exception e)
                    {
                        exceptions.Enqueue(e);
                    }
                });

            await Task.WhenAll(tasks);

            return exceptions.ToList();
        }
    }
}
