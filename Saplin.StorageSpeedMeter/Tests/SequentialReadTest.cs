using System;
using System.Diagnostics;

namespace Saplin.StorageSpeedMeter
{
    public class SequentialReadTest : SequentialTest
    {
        public SequentialReadTest(TestFile file, int blockSize, ICachePurger cachePurger = null) : base(file.ReadStream, blockSize, file.TestAreaSizeBytes/blockSize)
        {
            this.cachePurger = cachePurger;
        }

        public override string DisplayName { get => "Sequential read" + " [" + blockSize / 1024 / 1024 + "MB] block"; }

        protected override void DoOperation(byte[] buffer, Stopwatch sw)
        {
            sw.Restart();
            fileStream.Read(buffer, 0, blockSize);
            sw.Stop();
        }

        protected override byte[] InitBuffer()
        {
            if (fileStream.Length < blockSize) throw new ArgumentException("File size cant be less than block size");

            if (totalBlocks == 0) totalBlocks = (int)(fileStream.Length / blockSize);

            var buffer = new byte[blockSize];
            return buffer;
        }
    }
}
