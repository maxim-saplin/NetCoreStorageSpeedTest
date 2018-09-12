using System;
using System.Diagnostics;
using System.IO;

namespace Saplin.StorageSpeedMeter
{
    public class MemCopyTest : SequentialTest
    {
        private int[] src, dst;
        int current;

        public MemCopyTest(FileStream file, int blockSize, long totalBlocks = 0) : base(file, blockSize, totalBlocks)
        {
        }

        public override string DisplayName { get => "Memory copy" + " [" + blockSize / 1024 / 1024 + "MB] block"; }

        protected override void DoOperation(byte[] buffer, Stopwatch sw)
        {
            sw.Restart();
            Buffer.BlockCopy(src, 0, dst, current, src.Length);
            sw.Stop();
            current += blockSize;
        }

        protected override byte[] InitBuffer()
        {
            Status = TestStatus.InitMemBuffer;

            src = new int[blockSize / sizeof(int)];
            dst = new int[blockSize * totalBlocks / sizeof(int)];

            var rand = new Random();

            for (int i = 0; i < src.Length; i++)
                src[i] = rand.Next();

            current = 0;
            return null;
        }

        protected override void TestCompleted()
        {
            src = null;
            dst = null;

            base.TestCompleted();
        }
    }
}
