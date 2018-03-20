# NetCoreStorageSpeedTest
Class library (cross-platform, .NET Standard 2.0) for measuring read/write speeds of a storage and a console app for running tests on a given drive

# Building and Running
There's a NuGet dependepncy to ILLinker in WinMacDiskSpeedTest project (used for creating self-deployed .NET Packages - read "Release build-publish instructions.txt") which requires manually adding a NuGet repository (https://dotnet.myget.org/F/dotnet-core/api/v3/index.json). In order to build the project, this repository must be added, or the dependency can be removed (removing the possibility to make self-contained builds).
To add the repository go to "Tools -> Options -> NuGet Package Manager -> Package Source" and add a new source with "Source" address "https://dotnet.myget.org/F/dotnet-core/api/v3/index.json"
