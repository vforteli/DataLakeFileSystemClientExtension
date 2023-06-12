# DataLakeFileSystemClientExtension
Extension method for listing paths in parallel with Azure DataLakeFileSystemClient.
In Azure DataLakeGen2, Using the ListPathsAsync method on the DataLakeServiceClient can take tens of minutes or even hours with as little as hundreds of thousands of files across directories.

This extension method uses multiple threads to avoid calling the expensive recursive version of ListPathsAsync. This improves performance significantly, however the actual numbers varies depending on the directory structure.

## Benchmarks
No formal benchmarks provided yet. Actual improvements will vary depending on the folder structure targeted. With large folders the duration can however be decreased from hours to minutes.

## Installation
Build from source or download NuGet package: https://www.nuget.org/packages/vforteli.DataLakeClientExtensions

Target frameworks .Net 6 and .Net Standard 2.1

## Usage

List files in directory
``` csharp
  // List paths with IAsyncEnumerator
  var sourceFileSystemClient = new DataLakeServiceClient(new Uri(sourceConnection)).GetFileSystemClient("somefilesystem");
  await foreach (var path in sourceFileSystemClient.ListPathsParallelAsync("/"))       
  {
      // do something with PathItem
  } 
```

