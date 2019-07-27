using System;
using System.Diagnostics;

namespace Saplin.StorageSpeedMeter
{
    public class MemCopyTest : Test
    {
        private int[] src, dst;
        protected long totalBlocks;
        int current = 0;
        protected readonly int maxTestTime;
        Func<long> freeMem;
        Random rand = new Random();

        public long TotalBlocks
        {
            get { return totalBlocks; }
        }

        public MemCopyTest(int blockSize = 64*1024*1024, long totalBlocks = 96, Func<long> freeMem = null, int maxTestTimeSecs = 4)
        {
            if (blockSize <= 0) throw new ArgumentOutOfRangeException("blockSize", "Block size cant be negative");
            if (totalBlocks < 0) throw new ArgumentOutOfRangeException("totalBlocks", "Block number cant be negative");
            this.blockSize = blockSize;
            this.totalBlocks = totalBlocks;
            this.freeMem = freeMem;
            this.maxTestTime = maxTestTimeSecs;
        }

        public override string DisplayName { get => "Memory copy" + " [" + blockSize / 1024 / 1024 + "MB] block"; }

        public override TestResults Execute()
        {
            Status = TestStatus.Started;
            var results = new TestResults(this);

            try
            {
                Status = TestStatus.InitMemBuffer;
                CleanUp();
                InitBuffers();
            }
            catch
            {
                NotEnoughMemUpdate(results, 0);
                return results;
            }

            var sw = new Stopwatch();

            int prevPercent = -1;
            int curPercent = -1;

            RestartElapsed();

            Status = TestStatus.Running;

            current = 0;

            for (var i = 1; i < totalBlocks + 1; i++)
            {
                if (breakCalled)
                {
                    return results;
                }

                if (!DoOperation(sw)) return results;

                results.AddTroughputMbs(blockSize, 0, sw);

                //curPercent = (int)(i * 100 / totalBlocks);
                curPercent = Math.Min(
                    100,
                    Math.Max(
                        (int)(elapsedSw.ElapsedMilliseconds / 10 / maxTestTime),
                        curPercent = (int)(i * 100 / totalBlocks)
                    )
                );

                if (curPercent > prevPercent)
                {
                    Update(curPercent, results.GetLatest5AvgResult(), results: results);
                    prevPercent = curPercent;

                    if (curPercent == 100) break;
                }
            }

            results.TotalTimeMs = StopElapsed();

            FinalUpdate(results, ElapsedMs);

            CleanUp();

            return results;
        }

        protected bool DoOperation(Stopwatch sw)
        {
            int i;

            sw.Restart();
            //Buffer.BlockCopy(src, 0, dst, current, src.Length);
            Array.Copy(src, 0, dst, current, src.Length);
            //for (i = 0; i < src.Length; i++)
                //dst[i+current] = src[i];

            sw.Stop();
            current += src.Length;
            src[0] = rand.Next();

            if (current >= dst.Length)
            {
                for (i = 0; i < src.Length; i+=16)
                    src[i] = rand.Next();

                current = 0;
            }

            return true;
        }

        protected void InitBuffers()
        {
            long mem = Environment.Is64BitProcess ?  512 * 1024 * 1024 : 256 * 1024 * 1024;

            if (freeMem != null) mem = Math.Min(mem, freeMem()/10*7);
            var dstLength = blockSize * (mem / blockSize) / sizeof(int);

            if (dstLength < 4*blockSize / sizeof(int)) throw new OutOfMemoryException();

            dst = new int[dstLength];
            Array.Clear(dst, 0, dst.Length);

            src = new int[blockSize / sizeof(int)];
            for (int i = 0; i < src.Length; i+=4)
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
