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
        
#region Windows Memory Status
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
#endregion

        private static IntPtr sysCtlIntSize = (IntPtr)IntPtr.Size;

        public static UInt64 GetSysCtlIntegerByName(String name)
        {
            IntPtr sysCtlInt;
            sysctlbyname(name, out sysCtlInt, ref sysCtlIntSize, IntPtr.Zero, IntPtr.Zero);
            return (UInt64)sysCtlInt.ToInt64();
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

        public const string fileName = "CPDT_TestFile.dat";

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


        private static string[] macContainsExpcetions = { "/private/var", "/dev", "/System/Volumes/Data/home" };
        private static string[] linuxContainsExpcetions = { "/sys", "/snap", "/dev", "/run", "/proc" };
        //private static string[] androidIsExpcetions = { "/", "/vendor", "/firmware", "/dsp", "/persist", "/system", "/cache" };
        //private static string[] androidContainsExpcetions = { "/mnt/runtime", "/data/var", "/mnt/media_rw" };

        public static DriveInfo[] GetEligibleDrives()
        {
            var drives = DriveInfo.GetDrives().AsEnumerable();
                                      //.Where(d => d.IsReady
                                      //       && (d.DriveType == DriveType.Fixed
                                      //                          || d.DriveType == DriveType.Removable
                                      //                          || d.DriveType == DriveType.Unknown
                                      //                          || d.DriveType == DriveType.Network)
                                      //      );

            if (RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.OSX))
            {
                foreach (var e in macContainsExpcetions)
                    drives = drives.Where(d => !d.Name.Contains(e));
            }
            else if (RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows))
            {
            }
            else //Linux, no Android
            {
                //foreach (var e in androidContainsExpcetions)
                //    drives = drives.Where(d => !d.Name.Contains(e));
                foreach (var e in linuxContainsExpcetions)
                    drives = drives.Where(d => !d.Name.Contains(e));
                //foreach (var e in androidIsExpcetions)
                //    drives = drives.Where(d => d.Name.ToLower() != e.ToLower());
            }

            return drives.ToArray();
        }

        public static string GetTempFilePath(string drivePath)
        {
            string path = null;

            if (RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.OSX))
            {
                if (drivePath == "/" || drivePath == "/System/Volumes/Data") //Mac system disk
                {
                    path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), fileName);
                }
                else path = Path.Combine(drivePath, fileName);
            }
            else if (RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows))
            {
                var sysRoot = Path.GetPathRoot(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData));

                if (sysRoot == drivePath)
                {
                    path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), fileName); // Windows might not allow to write to system disk root
                }
                else path = Path.Combine(drivePath, fileName);
            }
            else // Linux
            {
                if (drivePath == "/home" || drivePath == "/")
                {
                    path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), fileName);
                }
                else path = Path.Combine(drivePath, fileName); 

                //else //Android
				//{
				//	if (drivePath == "/data") // Internal storage, use personal folder
				//	{
				//		path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), fileName);
				//	} 
				//	else path = Path.Combine(drivePath, fileName);
				//}
            }

            return path;
        }


    }
}
