using System;
using System.Diagnostics;
using System.IO;

namespace Saplin.StorageSpeedMeter
{
    public class SequentialReadTest : SequentialTest
    {
        private bool flushBuf = false;

        public SequentialReadTest(TestFile file, int blockSize, long totalBlocks = 0) : base(file.ReadStream, blockSize, totalBlocks)
        {
            flushBuf = file.enableMemCache;
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
