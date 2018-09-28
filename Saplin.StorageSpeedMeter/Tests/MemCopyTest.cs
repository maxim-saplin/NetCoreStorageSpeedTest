using System;
using System.Diagnostics;

namespace Saplin.StorageSpeedMeter
{
    public class MemCopyTest : Test
    {
        private int[] src, dst;
        protected long totalBlocks;
        int current = 0;

        public MemCopyTest(int blockSize = 64*1024*1024, long totalBlocks = 96)
        {
            if (blockSize <= 0) throw new ArgumentOutOfRangeException("blockSize", "Block size cant be negative");
            if (totalBlocks < 0) throw new ArgumentOutOfRangeException("totalBlocks", "Block number cant be negative");
            this.blockSize = blockSize;
            this.totalBlocks = totalBlocks;
        }

        public override string DisplayName { get => "Memory copy" + " [" + blockSize / 1024 / 1024 + "MB] block"; }

        public override TestResults Execute()
        {
            Status = TestStatus.Started;

            try
            {
                Status = TestStatus.InitMemBuffer;
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

            if (current >= dst.Length)
            {
                CleanUp();

                try
                {
                    InitBuffers();
                }
                catch
                {
                    Status = TestStatus.NotEnoughMemory;
                    return false;
                }

                current = 0;
            }

            return true;
        }

        protected void InitBuffers()
        {
            const int mem64 = 1280 * 1024 * 1024;
            const int mem32 = 640 * 1024 * 1024;

            src = new int[blockSize / sizeof(int)];
            dst = new int[(blockSize * (Environment.Is64BitProcess ? mem64/blockSize : mem32/blockSize)) / sizeof(int)];
            Array.Clear(dst, 0, dst.Length);

            var rand = new Random();

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
