using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Unity.Services.CloudSave.Authoring.Core.Batching
{
    /// <summary>An utility class for executing delegates in batches with a time interval between them</summary>
    /// <warning>Currently only supports async delegates (with or without return values)</warning>
    public static class Batching
    {
        const int k_BatchSize = 10;
        const double k_SecondsDelay = 1;

        const string k_BatchingExceptionMessage =
            "One or more exceptions were thrown during the batching execution. See inner exceptions.";

        /// <summary>
        /// Asynchronously execute a collection of delegates in batches with delay between them
        /// </summary>
        /// <param name="delegates">IEnumerable of the delegates you want to run in batches</param>
        /// <param name="callback">Callback that will be invoked when a delegate has finished executing</param>
        /// <param name="cancellationToken"></param>
        /// <param name="batchSize">Size of the batches</param>
        /// <param name="secondsDelay">Delay in seconds between batches</param>
        /// <exception cref="AggregateException">Exception thrown when a delegate throws an exception</exception>
        /// <warning>You need to handle the AggregateException's innerExceptions (that's where you'll get
        /// the exceptions related to the individual batch items executed)</warning>
        public static async Task ExecuteInBatchesAsync(
            IEnumerable<Func<Task>> delegates,
            CancellationToken cancellationToken,
            int batchSize = k_BatchSize,
            double secondsDelay = k_SecondsDelay)
        {
            var newDelegates = new List<Func<Task<int>>>();

            foreach (var del in delegates)
            {
                newDelegates.Add(
                    async () =>
                    {
                        await Task.Run(del, cancellationToken);
                        return 0;
                    });

                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }
            }

            await ExecuteInBatchesAsync(
                newDelegates,
                cancellationToken,
                batchSize,
                secondsDelay);
        }

        /// <summary>
        /// Asynchronously execute a collection of delegates in batches with delay between them
        /// </summary>
        /// <param name="delegates">IEnumerable of the delegates you want to run in batches</param>
        /// <param name="callback">Callback that will be invoked when a delegate has finished executing</param>
        /// <param name="cancellationToken"></param>
        /// <param name="batchSize">Size of the batches</param>
        /// <param name="secondsDelay">Delay in seconds between batches</param>
        /// <typeparam name="TResult">The return value type of your delegates</typeparam>
        /// <returns>A collection of results</returns>
        /// <exception cref="AggregateException">Exception thrown when a delegate throws an exception</exception>
        /// <warning>You need to handle the AggregateException's innerExceptions (that's where you'll get
        /// the exceptions related to the individual batch items executed)</warning>
        public static async Task<IReadOnlyList<TResult>> ExecuteInBatchesAsync<TResult>(
            IReadOnlyList<Func<Task<TResult>>> delegates,
            CancellationToken cancellationToken,
            int batchSize = k_BatchSize,
            double secondsDelay = k_SecondsDelay)
        {
            var exceptions = new ConcurrentQueue<Exception>();
            var batchesResult = new ConcurrentBag<TResult>();
            var chunks = delegates.Chunk(batchSize).ToList();

            for (int i = 0; i < chunks.Count; i++)
            {
                try
                {
                    var batchResult = await ExecuteBatchAsync(chunks[i], cancellationToken);
                    foreach (var result in batchResult)
                    {
                        batchesResult.Add(result);
                    }
                }
                catch (AggregateException e)
                {
                    foreach (var innerException in e.InnerExceptions)
                    {
                        exceptions.Enqueue(innerException);
                    }
                }

                if (i + 1 != chunks.Count)
                {
                    await Task.Delay(TimeSpan.FromSeconds(secondsDelay), cancellationToken);
                }

                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }
            }

            if (!exceptions.IsEmpty)
            {
                throw new AggregateException(k_BatchingExceptionMessage, exceptions.ToList());
            }

            return batchesResult.ToList();
        }

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

        static async Task<IReadOnlyList<TResult>> ExecuteBatchAsync<TResult>(
            IEnumerable<Func<Task<TResult>>> delegates,
            CancellationToken cancellationToken)
        {
            var exceptions = new ConcurrentQueue<Exception>();
            var batchesResult = new ConcurrentBag<TResult>();
            var tasks = new ConcurrentBag<Task>();

            Parallel.ForEach(
                delegates,
                del =>
                {
                    var task = Task.Run(
                        async () =>
                        {
                            try
                            {
                                var result = await Task.Run(del, cancellationToken);
                                batchesResult.Add(result);
                            }
                            catch (Exception e)
                            {
                                exceptions.Enqueue(e);
                            }
                        },
                        cancellationToken);

                    tasks.Add(task);
                });

            await Task.WhenAll(tasks);

            if (!exceptions.IsEmpty)
            {
                throw new AggregateException(k_BatchingExceptionMessage, exceptions.ToList());
            }

            return batchesResult.ToList();
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

        // Copy/pasted utility code from the Enumerable.Chunk method available in dotnet 5
        static IEnumerable<TSource[]> ChunkIterator<TSource>(IEnumerable<TSource> source, int size)
        {
            using IEnumerator<TSource> e = source.GetEnumerator();
            while (e.MoveNext())
            {
                TSource[] chunk = new TSource[size];
                chunk[0] = e.Current;

                int i = 1;
                for (; i < chunk.Length && e.MoveNext(); i++)
                {
                    chunk[i] = e.Current;
                }

                if (i == chunk.Length)
                {
                    yield return chunk;
                }
                else
                {
                    Array.Resize(ref chunk, i);
                    yield return chunk;
                    yield break;
                }
            }
        }

        static IEnumerable<TSource[]> Chunk<TSource>(this IEnumerable<TSource> source, int size)
            => ChunkIterator(source, size);
    }
}
