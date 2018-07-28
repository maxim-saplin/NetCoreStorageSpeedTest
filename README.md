# NetCoreStorageSpeedTest
Console Application (tested on Windows and MacOS) and a cross-platform Class Library (C#, .NET Standard 2.0) for measuring read/write speeds of disks/storage devices.

# Download Binaries
Of console apps at the relase section: https://github.com/maxim-saplin/NetCoreStorageSpeedTest/releases

# Publishing with IL Linker
The solution is configured to produce self-contained .NET Core Application as it's output. Effectively it puts all the dependencies of the framework and output binaries into one directory and let's run the console app without the need to have .NET Core installed (which is not the case for Mac).

There's a NuGet dependepncy to IL Linker in WinMacDiskSpeedTest project (used for creating self-contained .NET Packages - read "Release build-publish instructions.txt") which requires to manually add a NuGet repository (https://dotnet.myget.org/F/dotnet-core/api/v3/index.json). In order to build the project, this repository must be added, or the dependency can be removed (removing the possibility to make minimized self-contained builds).
To add the repository go to "Tools -> Options -> NuGet Package Manager -> Package Source" and add a new source with "Source" address "https://dotnet.myget.org/F/dotnet-core/api/v3/index.json"

macOS - IL Linker might fail to copy libMonoPosixHelper.dylib from Saplin.StorageSpeedMeter project's output to console app's outpu - do it manually
