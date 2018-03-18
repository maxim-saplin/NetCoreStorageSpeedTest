using System;
using System.Diagnostics;
using System.IO;

namespace Saplin.StorageSpeedMeter
{
    public class SequentialWriteTest : SequentialTest
    {
        public SequentialWriteTest(FileStream file, int blockSize, long totalBlocks, bool warmUp) : base(file, blockSize, totalBlocks, warmUp) { }

        public override string Name { get => "Sequential write" + " [" + blockSize / 1024 + "Kb] block"; }

        protected override void DoOperation(byte[] data, Stopwatch sw)
        {
            sw.Restart();
            file.Write(data, 0, blockSize);
            file.Flush();
            sw.Stop();
        }

        protected override byte[] InitTest()
        {
            if (totalBlocks == 0) throw new ArgumentOutOfRangeException("totalBlocks", "Block number cant be 0");

            Update("Initilizing data in memory");

            var data = new byte[blockSize];
            var rand = new Random();
            rand.NextBytes(data);
            return data;
        }
    }
}
