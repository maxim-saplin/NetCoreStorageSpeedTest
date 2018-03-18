using System;
using System.Diagnostics;
using System.IO;

namespace Saplin.StorageSpeedMeter
{
    public abstract class SequentialTest : Test
    {
        protected readonly FileStream file;
        protected readonly int blockSize;
        protected long totalBlocks;
        protected bool warmUp;
        protected readonly double warmUpBlocksPercentFromTotal = 0.05;

        public SequentialTest(FileStream file, int blockSize, long totalBlocks = 0, bool warmUp = false)
        {
            if (blockSize <= 0) throw new ArgumentOutOfRangeException("blockSize", "Block size cant be negative");
            if (totalBlocks < 0) throw new ArgumentOutOfRangeException("totalBlocks", "Block number cant be negative");

            this.file = file;
            this.blockSize = blockSize;
            this.totalBlocks = totalBlocks;
            this.warmUp = warmUp;
        }

        public override TestResults Execute()
        {
            byte[] data = InitTest();

            status = TestStatus.Started;

            Update("Executing sequential read test");

            var sw = new Stopwatch();
            var results = new TestResults(Test.unitMbs, Name, blockSize);

            int prevPercent = -1;
            int curPercent = -1;
            file.Seek(0, SeekOrigin.Begin);

            var elapsed = new Stopwatch();
            elapsed.Start();

            var warmUpBlocks = warmUp ? (int)Math.Ceiling(totalBlocks*warmUpBlocksPercentFromTotal) : 0;
            if (warmUp) Update("Warming up...");

            for (var i = 1 - warmUpBlocks; i < totalBlocks + 1; i++)
            {
                DoOperation(data, sw);

                if (i == 0) // final warm up block
                    file.Seek(0, SeekOrigin.Begin);

                if (i > 0) // not warm up blocks
                {

                    results.AddTroughputMbs(blockSize, file.Position - blockSize, sw);

                    if (breakCalled)
                    {
                        Update("Test interrupted");
                        return results;
                    }

                    curPercent = (int)(i * 100 / totalBlocks);
                    if (curPercent > prevPercent)
                    {
                        Update(string.Format("{0:0.00}{1}", results.GetLatestResult(), results.Unit), curPercent);
                        prevPercent = curPercent;
                    }
                }
            }

            elapsed.Stop();
            results.TotalTimeMs = elapsed.ElapsedMilliseconds;

            status = TestStatus.Completed;

            FinalUpdate(results, elapsed.ElapsedMilliseconds);

            return results;
        }

        protected abstract void DoOperation(byte[] data, Stopwatch sw);

        protected abstract byte[] InitTest();
    }
}
