using System;
using System.Diagnostics;

namespace Saplin.StorageSpeedMeter
{
    public class MemCopyTest : Test
    {
        private int[] src, dst;
        protected long totalBlocks;
        int current = 0;
        Func<long> freeMem;
        Random rand = new Random();

        public MemCopyTest(int blockSize = 64*1024*1024, long totalBlocks = 96, Func<long> freeMem = null)
        {
            if (blockSize <= 0) throw new ArgumentOutOfRangeException("blockSize", "Block size cant be negative");
            if (totalBlocks < 0) throw new ArgumentOutOfRangeException("totalBlocks", "Block number cant be negative");
            this.blockSize = blockSize;
            this.totalBlocks = totalBlocks;
            this.freeMem = freeMem;
        }

        public override string DisplayName { get => "Memory copy" + " [" + blockSize / 1024 / 1024 + "MB] block"; }

        public override TestResults Execute()
        {
            Status = TestStatus.Started;

            try
            {
                Status = TestStatus.InitMemBuffer;
                CleanUp();
                InitBuffers();
            }
            catch
            {
                Status = TestStatus.NotEnoughMemory;
                return null;
            }

            var sw = new Stopwatch();
            var results = new TestResults(this);

            int prevPercent = -1;
            int curPercent = -1;

            RestartStopwatch();

            Status = TestStatus.Running;

            for (var i = 1; i < totalBlocks + 1; i++)
            {
                if (breakCalled)
                {
                    return results;
                }

                if (!DoOperation(sw)) return results;

                results.AddTroughputMbs(blockSize, 0, sw);

                curPercent = (int)(i * 100 / totalBlocks);
                if (curPercent > prevPercent)
                {
                    Update(curPercent, results.GetLatest5AvgResult());
                    prevPercent = curPercent;
                }
            }

            results.TotalTimeMs = StopStopwatch();

            FinalUpdate(results, ElapsedMs);

            CleanUp();

            return results;
        }

        protected bool DoOperation(Stopwatch sw)
        {
            var rand = new Random();

            sw.Restart();
            Buffer.BlockCopy(src, 0, dst, current, src.Length);
            sw.Stop();
            current += blockSize / sizeof(int);
            src[0] = rand.Next();

            if (current >= dst.Length)
            {
                //CleanUp();

                //try
                //{
                //    InitBuffers();
                //}
                //catch
                //{
                //    Status = TestStatus.NotEnoughMemory;
                //    return false;
                //}
                for (int i = 0; i < src.Length; i+=16)
                    src[i] = rand.Next();

                current = 0;
            }

            return true;
        }

        protected void InitBuffers()
        {
            long mem = Environment.Is64BitProcess ?  1280 * 1024 * 1024 : 640 * 1024 * 1024;

            if (freeMem != null) mem = Math.Min(mem, freeMem()/10*9);

            src = new int[blockSize / sizeof(int)];
            dst = new int[blockSize * (mem / blockSize) / sizeof(int)];
            Array.Clear(dst, 0, dst.Length);

            for (int i = 0; i < src.Length; i++)
                src[i] = rand.Next();

        }

        protected void CleanUp()
        {
            src = null;
            dst = null;
            GC.Collect(2, GCCollectionMode.Forced, true);
        }
    }
}
