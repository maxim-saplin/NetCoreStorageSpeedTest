using System;
using System.Diagnostics;

namespace Saplin.StorageSpeedMeter
{
    public class SequentialWriteTest : SequentialTest
    {
        private bool flushBuf = false;

        public SequentialWriteTest(TestFile file, int blockSize, bool warmUp) : base(file.WriteStream, blockSize, file.TestAreaSizeBytes/blockSize, warmUp)
        {
            flushBuf = !file.writeBuffering;
        }

        public override string DisplayName { get => "Sequential write" + " [" + blockSize / 1024 / 1024 + "MB] block"; }

        protected override void DoOperation(byte[] buffer, Stopwatch sw)
        {
            sw.Restart();
            fileStream.Write(buffer, 0, blockSize);
            if (flushBuf) fileStream.Flush(true);
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
