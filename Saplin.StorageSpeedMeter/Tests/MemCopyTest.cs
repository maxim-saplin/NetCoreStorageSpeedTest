using System;
using System.Diagnostics;
using System.IO;

namespace Saplin.StorageSpeedMeter
{
    public class MemCopyTest : SequentialTest
    {
        private byte[] src, dst;
        int current;

        public MemCopyTest(FileStream file, int blockSize, long totalBlocks = 0) : base(file, blockSize, totalBlocks)
        {
            src = new byte[blockSize];
            dst = new byte[blockSize * totalBlocks];
        }

        public override string Name { get => "Memory copy" + " [" + blockSize / 1024 + "Kb] block"; }

        protected override void DoOperation(byte[] buffer, Stopwatch sw)
        {
            sw.Restart();
            src.CopyTo(dst, current);
            sw.Stop();
            current += blockSize;
        }

        protected override byte[] InitBuffer()
        {
            var rand = new Random();
            rand.NextBytes(src);
            current = 0;
            return src;
        }
    }
}
