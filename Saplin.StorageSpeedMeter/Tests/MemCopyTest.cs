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
            src = new int[blockSize/sizeof(int)];
            dst = new int[blockSize * totalBlocks / sizeof(int)];
        }

        public override string Name { get => "Memory copy" + " [" + blockSize / 1024 + "Kb] block"; }

        protected override void DoOperation(byte[] buffer, Stopwatch sw)
        {
            sw.Restart();
            Buffer.BlockCopy(src, 0, dst, current, src.Length);
            sw.Stop();
            current += blockSize;
        }

        protected override byte[] InitBuffer()
        {
            var rand = new Random();

            for (int i = 0; i < src.Length; i++)
                src[i] = rand.Next();

            current = 0;
            return null;
        }
    }
}
