using System;
using System.Diagnostics;
using System.IO;

namespace Saplin.StorageSpeedMeter
{
    public class SequentialWriteTest : SequentialTest
    {
        public SequentialWriteTest(FileStream file, int blockSize, long totalBlocks, bool warmUp) : base(file, blockSize, totalBlocks, warmUp) { }

        public override string DisplayName { get => "Sequential write" + " [" + blockSize / 1024 / 1024 + "MB] block"; }

        protected override void DoOperation(byte[] buffer, Stopwatch sw)
        {
            sw.Restart();
            file.Write(buffer, 0, blockSize);
            file.Flush();
            sw.Stop();
        }

        protected override byte[] InitBuffer()
        {
            if (totalBlocks == 0) throw new ArgumentOutOfRangeException("totalBlocks", "Block number cant be 0");

            Status = TestStatus.InitMemBuffer;
            //Update("Initilizing data in memory");

            var buffer = new byte[blockSize];
            var rand = new Random();
            rand.NextBytes(buffer);
            return buffer;
        }
    }
}
