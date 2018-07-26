using Saplin.StorageSpeedMeter;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace WinMacDiskSpeedTest
{
    class Program
    {
        private static string PickDrive(long freeSpace)
        {
            var i = 0;

            var drives = RamDiskUtil.GetEligibleDrives();

            foreach (var d in drives)
            {
                var flag = false;

                if (d.TotalFreeSpace < freeSpace) flag = true; else i++;

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

                if (Console.KeyAvailable && Console.ReadKey(true).Key == ConsoleKey.Escape) return null;

                var input = Console.ReadLine();

                if (!Int32.TryParse(input, out index)) index = -1;
            } while ((index < 1) || (index > i));

            index--;

            return drives[index].Name;
        }

        static void Main(string[] args)
        {
            try
            {
                Console.WriteLine("STORAGE SPEED TEST\n");
                Console.WriteLine("\nTotal RAM: {0:0.00}Gb, Available RAM: {1:0.00}Gb\n", (double)RamDiskUtil.TotalRam / 1024 / 1024 / 1024, (double)RamDiskUtil.FreeRam / 1024 / 1024 / 1024);

                var drivePath = PickDrive(BigTest.FreeSpaceRequired);

                if (drivePath == null) return;

                var testSuite = new BigTest(drivePath);

                Console.WriteLine("Test file: {0}, Size: {1:0.00}Gb\n\n Press ESC to break", testSuite.FilePath, (double)testSuite.FileSize / 1024 / 1024 / 1024);

                string currentTest = null;
                const int curCursor = 40;
                var breakTest = false;

                testSuite.StatusUpdate += (sender, e) =>
                {
                    if ((sender as Test).Name != currentTest)
                    {
                        currentTest = (sender as Test).Name;
                        Console.Write("\n{0}/{1} {2}", testSuite.CompletedTests + 1, testSuite.TotalTests, (sender as Test).Name);
                    }

                    ClearLine(curCursor);

                    if (e.ProgressPercent != null)
                        Console.Write("{0}% {2} {1} ", e.ProgressPercent, e.Message, GetNextAnimation());
                    else Console.Write(e.Message);

                    if (Console.KeyAvailable && Console.ReadKey(true).Key == ConsoleKey.Escape)
                    {
                        Console.WriteLine("Stopping...");
                        testSuite.Break();
                        breakTest = true;
                    }

                    ShowCounters(testSuite);
                };

                var results = testSuite.Execute();

                if (!breakTest)
                {
                    Console.WriteLine("\n\tWrite Score: {0:0.00} MB/s,\t Read score: {1:0.00} MB/s", testSuite.WriteScore, testSuite.ReadScore);
                    Console.WriteLine("\n\nTest file deleted.  Saving results to CSV files in folder: " + testSuite.ResultsFolderPath);
                    testSuite.ExportToCsv(testSuite.ResultsFolderPath, true);
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine("\nProgram interupted due to unexpected error:");
                Console.WriteLine("\t" + ex.GetType() + " " + ex.Message);
            }


            if (!RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.OSX))
            {
                Console.WriteLine("\nPress any key to quit");
                Console.ReadKey();
            }
        }

        private static void ClearLine(int cursorLeft)
        {
            Console.CursorLeft = cursorLeft;
            Console.Write(new string(' ', Console.WindowWidth - cursorLeft - 1));
            Console.CursorLeft = cursorLeft;
        }

        static char[] anim = new char[] { '/', '|', '\\', '-', '/', '|', '\\', '-' };
        static int animCounter = 0;

        private static char GetNextAnimation()
        {
            animCounter++;
            return anim[animCounter % anim.Length];
        }

        static long prevElapsedSecs = 0;

        private static void ShowCounters(TestSuite ts)
        {
            var left = Console.CursorLeft;
            var top = Console.CursorTop;
            var elapsedSecs = ts.ElapsedMs / 1000;

            if (prevElapsedSecs != elapsedSecs)
            {
                var elapsed = string.Format("                          Elapsed time: {0:00}m {1:00}s", elapsedSecs / 60, elapsedSecs % 60);
                Console.CursorLeft = Console.WindowWidth - elapsed.Length -1;
                Console.CursorTop = 0;
                Console.Write(elapsed);

                var remaing = string.Format("                          Remaining time: {0:00}m {1:00}s", ts.RemainingMs / 1000 / 60, ts.RemainingMs / 1000 % 60);
                Console.CursorLeft = Console.WindowWidth - remaing.Length -1;
                Console.CursorTop = 1;
                Console.Write(remaing);

                prevElapsedSecs = elapsedSecs;
            }

            Console.CursorLeft = left;
            Console.CursorTop = top;
        }

        public static void WriteLineWordWrap(string text, int tabSize = 8)
        {
            string[] lines = text
                .Replace("\t", new String(' ', tabSize))
                .Split(new string[] { Environment.NewLine }, StringSplitOptions.None);

            for (int i = 0; i < lines.Length; i++)
            {
                string process = lines[i];
                List<String> wrapped = new List<string>();

                while (process.Length > Console.WindowWidth)
                {
                    int wrapAt = process.LastIndexOf(' ', Math.Min(Console.WindowWidth - 1, process.Length));
                    if (wrapAt <= 0) break;

                    wrapped.Add(process.Substring(0, wrapAt));
                    process = process.Remove(0, wrapAt + 1);
                }

                foreach (string wrap in wrapped)
                {
                    Console.WriteLine(wrap);
                }

                Console.WriteLine(process);
            }
        }
    }
}