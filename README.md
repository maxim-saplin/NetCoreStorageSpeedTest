# NetCoreStorageSpeedTest
Console Application (Linux, Windows and MacOS - .NET Core 3.1) and a cross-platform Class Library (C#, .NET Standard 2.1) for measuring read/write speeds of disks/storage devices.

The class library is the perfomance measuring core of CPDT Benchmark (Cross Platform Disk Test): https://github.com/maxim-saplin/CrossPlatformDiskTest

There's a console app (started via Terminal) which runs same tests as CPDT, though only with default options and no way to change them.

# Download 
Linux: https://github.com/maxim-saplin/NetCoreStorageSpeedTest/releases/latest/download/CPDT_Linux.zip
- extract executable from the archive and run it in Terminal (./CPDT_Console_x64)
- Includes Intel x64 and ARM 32-bit builds (works in Raspberry Pi)

Older Win/Mac versions: https://github.com/maxim-saplin/NetCoreStorageSpeedTest/releases/tag/1.1.5

# Building
Build using `dotnet`:
```
dotnet publish CPDT_Console/CPDT_Console.csproj -c release -r linux-x64 -o output --self-contained true /p:PublishSingleFile=true
```

You may replace `linux-x64` with the platform you're building for.

You may also try to use Visual Stuio (Win or Mac) and build/run "CPDT_Console" project from within IDE
