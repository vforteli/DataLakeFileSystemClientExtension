using Azure.Storage.Files.DataLake;
using DataLakeFileSystemClientExtension;
using System.Diagnostics;


using var cancellationTokenSource = new CancellationTokenSource();

Console.CancelKeyPress += (s, e) =>
{
    if (!cancellationTokenSource.IsCancellationRequested)
    {
        e.Cancel = true;
        Console.WriteLine("Breaking, waiting for queued tasks to complete. Press break again to force stop");
        cancellationTokenSource.Cancel();
    }
    else
    {
        Console.WriteLine("Terminating threads");
        Environment.Exit(1);
    }
};

var stopwatch = Stopwatch.StartNew();

var connectionString = "somesastoken";
var sourceFileSystemClient = new DataLakeServiceClient(new Uri(connectionString)).GetFileSystemClient("stuff");


var processedCount = 0;

await foreach (var path in sourceFileSystemClient.ListPathsParallelAsync("/", cancellationToken: cancellationTokenSource.Token))
{
    // this could also handle the paths in parallel
    var currentCount = Interlocked.Increment(ref processedCount);
    if (currentCount % 1000 == 0)
    {
        Console.WriteLine($"Processed {currentCount} files and directories... {processedCount / (stopwatch.ElapsedMilliseconds / 1000f)} fps");
    }
}

Console.WriteLine($"Done, took {stopwatch.Elapsed}");
Console.WriteLine($"Found {processedCount} files and directories");
