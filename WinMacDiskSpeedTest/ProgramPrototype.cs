using System;
using System.IO;
using System.Diagnostics;
using System.Linq;
using NickStrupat;

namespace WinMacDiskSpeedTest
{
    class ProgramPrototype
    {
        private static void LegacyTest(string path)
        {
            Console.WriteLine("Creating file: {0}", path);

            FileStream file;

            try
            {
                file = File.Create(path, fileStreamBufSize, FileOptions.WriteThrough);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Can't create file. \n" + ex.Message);

                return;
            }

            Console.WriteLine("Creating test data in memory...\n");

            var data = new byte[blockSize];
            var rand = new Random();

            rand.NextBytes(data);

            var sw = new Stopwatch();
            var ci = new ComputerInfo();
            var fileSizeToBlockSizeRatio = (int)(ci.TotalPhysicalMemory / blockSize) + 1;

            try
            {
                Console.Write("Sequential write: \t\t");
                var curCursor = Console.CursorLeft;
                double throughput;

                var sw2 = new Stopwatch();

                sw.Restart();

                var anim = new char[] { '/', '|', '\\', '-', '/', '|', '\\', '-' };

                for (var i = 0; i < fileSizeToBlockSizeRatio; i++)
                {
                    sw2.Restart();
                    file.Write(data, 0, data.Length);
                    file.Flush();
                    sw2.Stop();
                    throughput = ((double)blockSize / 1024 / 1024) / (double)(sw2.ElapsedMilliseconds == 0 ? 1 : sw2.ElapsedMilliseconds) * 1000;

                    Console.CursorLeft = curCursor;
                    Console.Write("{0:0.00} [MB/s] {2} Testing in progress... {1}% ready",
                        throughput,
                        i * 100 / fileSizeToBlockSizeRatio,
                       anim[i % anim.Length]);
                }

                sw.Stop();

                throughput = ((double)blockSize / 1024 / 1024 * fileSizeToBlockSizeRatio) / (double)(sw.ElapsedMilliseconds == 0 ? 1 : sw.ElapsedMilliseconds) * 1000;
                Console.CursorLeft = curCursor;
                Console.WriteLine("{0:0.00} [MB/s]\t\t\t\t\t", throughput);

                file.Close();


                file = File.Open(path, FileMode.Open);

                Console.Write("Sequential read: \t\t");
                curCursor = Console.CursorLeft;


                sw.Restart();


                for (var i = 0; i < fileSizeToBlockSizeRatio; i++)
                {
                    sw2.Restart();
                    file.Read(data, 0, data.Length);
                    sw2.Stop();
                    throughput = ((double)blockSize / 1024 / 1024) / (double)(sw2.ElapsedMilliseconds == 0 ? 1 : sw2.ElapsedMilliseconds) * 1000;

                    Console.CursorLeft = curCursor;
                    Console.Write("{0:0.00} [MB/s] {2} Testing in progress... {1}% ready",
                        throughput,
                        i * 100 / fileSizeToBlockSizeRatio,
                        anim[i % anim.Length]);
                }

                sw.Stop();

                throughput = ((double)blockSize / 1024 / 1024 * fileSizeToBlockSizeRatio) / (double)(sw.ElapsedMilliseconds == 0 ? 1 : sw.ElapsedMilliseconds) * 1000;
                Console.CursorLeft = curCursor;
                Console.WriteLine("{0:0.00} [MB/s]\t\t\t\t\t", throughput);
            }
            finally
            {
                file?.Dispose();
                File.Delete(path);
            }
        }

        private static string GetFilePath()
        {
            var driveRoot = PickDrive();
            return FileOnDrive(driveRoot);
        }

        private static string PickDrive()
        {
            var ci = new ComputerInfo();
            var i = 0;

            var drives = DriveInfo.GetDrives()
                                  .Where(d => d.IsReady && (d.DriveType == DriveType.Fixed
                                                            || d.DriveType == DriveType.Removable
                                                            || d.DriveType == DriveType.Unknown))
                                  .ToArray();

            foreach (var d in drives)
            {
                var flag = false;

                if ((ulong)d.TotalFreeSpace < ci.TotalPhysicalMemory) flag = true; else i++;

                Console.WriteLine(
                    "[{0}] {1} {2:0.00} Gb free {3}",
                        flag ? " " : i.ToString(),
                        d.Name,
                        (double)d.TotalFreeSpace / 1024 / 1024 / 1024,
                        flag ? "- insufficient free space" : ""
                        );
            }

            Console.Write("Please pick drive to test: ");

            int index;
            do
            {
                var input = Console.ReadLine();

                if (!Int32.TryParse(input, out index)) index = -1;
            } while ((index < 1) || (index > i));

            index--;

            return drives[index].Name;
        }

        private static string FileOnDrive(string driveRoot)
        {
            string path;
            if (driveRoot == "/")
                path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), fileName);
            else
            {
                var sysRoot = Path.GetPathRoot(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData));

                if (sysRoot == driveRoot) path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), fileName);
                else path = Path.Combine(driveRoot, fileName);
            }

            return path;
        }

        const int blockSize = 256 * 1024 * 1024; //MBs
        const int fileStreamBufSize = 4 * 1024;
        const string fileName = "WinMacDiskSpeedTest_TestFile.dat";

        static void Main2(string[] args)
        {
            var ci = new ComputerInfo();

            Console.WriteLine("STORAGE SPEED TEST\n");
            Console.WriteLine("Available RAM: {0:0.00}Gb", (double)ci.TotalPhysicalMemory / 1024 / 1024 / 1024);
            Console.WriteLine("Testing might take a while on systems with much RAM. Modern operating systems use file caching in memory. Which means that with plenty of RAM test data, generated by this app and written to disk, will stay in memory and read tests will be severely corrupted. For the sake of accurate evaluation of storage perfromance (and not RAM), test data file will be created of a size equivalent to system RAM. This will ensure RAM caching won't kick in.\n");

            var path = "";

            path = GetFilePath();

            LegacyTest(path);

            Console.WriteLine("Done. Press any key to quit");

            Console.ReadKey();
        }
    }
}