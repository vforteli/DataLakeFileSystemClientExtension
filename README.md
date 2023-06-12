# DataLakeFileSystemClientExtension
Extension method for listing paths in parallel with Azure DataLakeFileSystemClient.
In Azure DataLakeGen2, Using the ListPathsAsync method on the DataLakeServiceClient can take tens of minutes or even hours with as little as hundreds of thousands of files across directories.

This extension method uses multiple threads to avoid calling the expensive recursive version of ListPathsAsync. This improves performance significantly, however the actual numbers varies depending on the directory structure.

# Installation
Build from source or download NuGet package: https://www.nuget.org/packages/vforteli.DataLakeClientExtensions

Target frameworks .Net 6 and .Net Standard 2.1

# Usage

List files in directory
``` csharp
  // Search with default options, substitutions, insertions, deletions and default maximum distance (3)
  var results = FuzzySearch.Find("sometext", "here is someteext for you");   
  
  // Search with specified maximum distance
  var results = FuzzySearch.Find("sometext", "here is someteext for you", 1);  
    
  
```
