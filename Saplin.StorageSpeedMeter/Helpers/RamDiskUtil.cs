using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace Saplin.StorageSpeedMeter
{
    public class RamDiskUtil
    {
        internal struct MemStatus
        {
            internal UInt32 dwLength;
            internal UInt32 dwMemoryLoad;
            internal UInt64 ullTotalPhys;
            internal UInt64 ullAvailPhys;
            internal UInt64 ullTotalPageFile;
            internal UInt64 ullAvailPageFile;
            internal UInt64 ullTotalVirtual;
            internal UInt64 ullAvailVirtual;
            internal UInt64 ullAvailExtendedVirtual;
        }

        [DllImport("Kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern Boolean GlobalMemoryStatusEx(ref MemStatus lpBuffer);

        private static MemStatus memStatus = new MemStatus();

        private static void UpdateWindowsMemStatus()
        {
            if (!GlobalMemoryStatusEx(ref memStatus)) throw new Win32Exception("Error getting Windows memory info");
        }

        private static IntPtr lineSizeSize = (IntPtr)IntPtr.Size;

        public static UInt64 GetSysCtlIntegerByName(String name)
        {
            sysctlbyname(name, out var lineSize, ref lineSizeSize, IntPtr.Zero, IntPtr.Zero);
            return (UInt64)lineSize.ToInt64();
        }
        
        [DllImport("libc")]
        private static extern Int32 sysctlbyname(String name, out IntPtr oldp, ref IntPtr oldlenp, IntPtr newp, IntPtr newlen);

        static RamDiskUtil()
        {
            if (RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows))
            {
                memStatus.dwLength = checked((UInt32)Marshal.SizeOf(typeof(MemStatus)));
                UpdateWindowsMemStatus();
                TotalRam = (long)memStatus.ullTotalPhys;
            }
            else if (RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.OSX))
            {
                TotalRam = (long)GetSysCtlIntegerByName("hw.memsize");
            }
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
                {
                    UpdateWindowsMemStatus();
                    return (long)memStatus.ullAvailPhys;
                }

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
