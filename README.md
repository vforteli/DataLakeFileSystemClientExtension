# DataLakeFileSystemClientExtension
Extension method for listing paths in parallel with Azure DataLakeFileSystemClient.
In Azure DataLakeGen2, Using the ListPathsAsync method on the DataLakeServiceClient can take tens of minutes or even hours with as little as hundreds of thousands of files across directories.

This extension method uses multiple threads to avoid calling the expensive recursive version of ListPathsAsync. This improves performance significantly, however the actual numbers varies depending on the directory structure.

## Benchmarks
The not so scientific benchmarks have been run on a storage account containing one filesystem containing 32 folders, each folder contains 1600 subfolders and one file and each subfolder contains 10 files.  
Total files and folders: 563234.

Tests run on an MacBook Pro M2 with 100/10 mbit connection against an Azure Storage Account with Standard SKU and hierarchical namespace enabled (Datalakegen2).


## Installation
Build from source or download NuGet package: https://www.nuget.org/packages/vforteli.DataLakeClientExtensions

Target frameworks .Net 6 and .Net Standard 2.1

## Usage

List files in directory
``` csharp
  // List paths with IAsyncEnumerable
  var sourceFileSystemClient = new DataLakeServiceClient(new Uri(sourceConnection)).GetFileSystemClient("somefilesystem");
  await foreach (var path in sourceFileSystemClient.ListPathsParallelAsync("/"))       
  {
      // do something with PathItem
  } 
```

