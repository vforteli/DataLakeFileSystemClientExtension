using System;
using Azure.Storage.Files.DataLake;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace AzCopySubstitute;

/// <summary>
/// Extension method for listing paths using many threads
/// </summary>
public static class DataLakeFileSystemClientExtension
{
    /// <summary>
    /// List paths recursively using multiple thread for top level directories
    /// </summary>
    /// <param name="dataLakeFileSystemClient">Authenticated filesystem client where the search should start</param>
    /// <param name="searchPath">Directory where recursive listing should start</param>
    /// <param name="paths">BlockingCollection where paths will be stored</param>
    /// <param name="maxThreads">Max degrees of parallelism</param>
    /// <param name="cancellationToken"></param>
    /// <returns>Task which completes when all items have been added to the blocking collection</returns>
    public static Task ListPathsParallelAsync(this DataLakeFileSystemClient dataLakeFileSystemClient, string searchPath, BlockingCollection<string> paths, int maxThreads = 256, CancellationToken cancellationToken = default) => Task.Run(async () =>
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
                                if (!childPath.IsDirectory ?? false)
                                {
                                    paths.Add(childPath.Name);
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
}
