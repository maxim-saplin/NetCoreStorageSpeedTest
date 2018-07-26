using NickStrupat;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace Saplin.StorageSpeedMeter
{
    public class RamDiskUtil
    {
        private static ComputerInfo ci;

        static RamDiskUtil()
        {
            ci = new ComputerInfo();

            TotalRam = (long)ci.TotalPhysicalMemory;
        }

        public const string fileName = "WinMacDiskSpeedTest_TestFile.dat";

        public static long TotalRam
        {
            get;
        }

        public static long FreeRam
        {
            get
            {
                if (RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows))
                    return (long)ci.AvailablePhysicalMemory;

                var totalMemUsed = Process.GetProcesses().AsQueryable<Process>().Select<Process, long>(p => p.WorkingSet64).Sum();

                return TotalRam - totalMemUsed > 0 ? TotalRam - totalMemUsed : 0;
            }
        }

        public static DriveInfo[] GetEligibleDrives()
        {
            return DriveInfo.GetDrives()
                                      .Where(d => d.IsReady 
                                             && (d.DriveType == DriveType.Fixed
                                                                || d.DriveType == DriveType.Removable
                                                                || d.DriveType == DriveType.Unknown)  
                                             && (!d.Name.Contains("/private/var/vm") && !d.DriveFormat.Contains("osxfuse")) // macOS virtual drive
                                            ).ToArray();
        }

        public static string GetTempFilePath(string drivePath)
        {
            string path;

            if (drivePath == "/") //Mac system disk
                path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), fileName);
            else 
            {
                var sysRoot = Path.GetPathRoot(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData));

                if (sysRoot == drivePath) path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), fileName); // Windows
                else path = Path.Combine(drivePath, fileName);
            }

            return path;
        }


    }
}
