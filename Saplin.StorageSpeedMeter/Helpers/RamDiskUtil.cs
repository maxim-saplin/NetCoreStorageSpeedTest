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

        //struct vmtotal
        //{
        //    short t_rq;     /* length of the run queue */
        //    short t_dw;     /* jobs in ``disk wait'' (neg priority) */
        //    short t_pw;     /* jobs in page wait */
        //    short t_sl;     /* jobs sleeping in core */
        //    short t_sw;     /* swapped out runnable/short block jobs */
        //    long t_vm;      /* total virtual memory */
        //    long t_avm;     /* active virtual memory */
        //    long t_rm;      /* total real memory in use */
        //    long t_arm;     /* active real memory */
        //    long t_vmshr;   /* shared virtual memory */
        //    long t_avmshr;  /* active shared virtual memory */
        //    long t_rmshr;   /* shared real memory */
        //    long t_armshr;  /* active shared real memory */
        //    long t_free;        /* free memory pages */
        //};

        //private static IntPtr vmtotalSize;

        //private static vmtotal GetSysCtlVmTotal()
        //{
        //    vmtotalSize = (IntPtr)Marshal.SizeOf(typeof(vmtotal));
        //    IntPtr vmtotalPtr = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(vmtotal)));

        //    var result = sysctlbyname("vm.vmtotal", out vmtotalPtr, ref vmtotalSize, IntPtr.Zero, IntPtr.Zero);

        //    var vmtotal = (vmtotal)Marshal.PtrToStructure(vmtotalPtr, typeof(vmtotal)); 

        //    return vmtotal;
        //}

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

        public static DriveInfo[] GetEligibleDrives()
        {
            return DriveInfo.GetDrives()
                                      .Where(d => d.IsReady 
                                             && (d.DriveType == DriveType.Fixed
                                                                || d.DriveType == DriveType.Removable
                                                                || d.DriveType == DriveType.Unknown)  
                                             && (!d.Name.Contains("/private/var/vm")) // macOS virtual drive
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
