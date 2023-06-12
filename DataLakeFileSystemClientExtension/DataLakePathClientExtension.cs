using System;
using Azure.Storage.Files.DataLake;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Azure.Storage.Files.DataLake.Models;

namespace DataLakeFileSystemClientExtension;

/// <summary>
/// Extension method for listing paths using many threads
/// </summary>
public static class DataLakeFileSystemClientExtension
{
    /// <summary>
    /// List paths recursively using multiple threads
    /// Fills provided blocking collection with PathItems
    /// </summary>
    /// <param name="dataLakeFileSystemClient">Authenticated filesystem client where the search should start</param>
    /// <param name="searchPath">Directory where recursive listing should start</param>
    /// <param name="paths">BlockingCollection where paths will be stored</param>
    /// <param name="maxThreads">Max degrees of parallelism. Typically use something like 256</param>
    /// <param name="cancellationToken"></param>
    /// <returns>Task which completes when all items have been added to the blocking collection</returns>
    public static Task ListPathsParallelAsync(this DataLakeFileSystemClient dataLakeFileSystemClient, string searchPath, BlockingCollection<PathItem> paths, int maxThreads = 256, CancellationToken cancellationToken = default) => Task.Run(async () =>
    {
        var filesCount = 0;
        using var directoryPaths = new BlockingCollection<string>();
        var tasks = new ConcurrentDictionary<Guid, Task>();

        using var semaphore = new SemaphoreSlim(maxThreads, maxThreads);

        directoryPaths.Add(searchPath);

        try
        {
            while (!directoryPaths.IsCompleted)
            {
                await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

                if (directoryPaths.TryTake(out var directoryPath, Timeout.Infinite, cancellationToken))
                {
                    var taskId = Guid.NewGuid();
                    tasks.TryAdd(taskId, Task.Run(async () =>
                    {
                        try
                        {
                            await foreach (var childPath in dataLakeFileSystemClient.GetPathsAsync(directoryPath, recursive: false, cancellationToken: cancellationToken).ConfigureAwait(false))
                            {
                                paths.Add(childPath);

                                if (!childPath.IsDirectory ?? false)
                                {
                                    var currentCount = Interlocked.Increment(ref filesCount);
                                }
                                else
                                {
                                    directoryPaths.Add(childPath.Name);
                                }
                            }
                        }
                        catch (TaskCanceledException) { }
                        finally
                        {
                            tasks.TryRemove(taskId, out _);
                            semaphore.Release();

                            if (tasks.IsEmpty && directoryPaths.Count == 0)
                            {
                                directoryPaths.CompleteAdding();
                            }
                        }
                    }, cancellationToken));
                }
            }
        }
        catch (TaskCanceledException) { }
        finally
        {
            paths.CompleteAdding();
        }
    });


    /// <summary>
    /// List paths recursively using multiple threads
    /// </summary>
    /// <param name="dataLakeFileSystemClient"></param>
    /// <param name="searchPath"></param>
    /// <param name="maxThreads"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public static async IAsyncEnumerable<PathItem> ListPathsParallelAsync(this DataLakeFileSystemClient dataLakeFileSystemClient, string searchPath, int maxThreads = 256, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var paths = new BlockingCollection<PathItem>();

        var task = dataLakeFileSystemClient.ListPathsParallelAsync(searchPath, paths, maxThreads, cancellationToken);

        while (paths.TryTake(out var path, -1, cancellationToken))
        {
            yield return path;
        }

        await task;
    }
}
