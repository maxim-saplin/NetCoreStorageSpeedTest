using System.Diagnostics;
using System.IO;

namespace Saplin.StorageSpeedMeter
{
    public class RandomReadTest : RandomTest
    {
        private long fileSize;

        public RandomReadTest(TestFile file, int blockSize, int testTimeSecs = 30, ICachePurger cachePurger = null) : base(file.ReadStream, blockSize, testTimeSecs)
        {
            this.cachePurger = cachePurger;
            fileSize = file.TestAreaSizeBytes;
        }

        public override string DisplayName { get => "Random read" + " [" + blockSize / 1024 + "KB] block"; }

        protected override void ValidateAndInitParams()
        {
            base.ValidateAndInitParams();

            maxBlock = (fileSize / 2 / blockSize)-1;
            minBlock = 0;
        }

        protected override void DoOperation(byte[] buffer, Stopwatch sw, long offsetBytes, int i)
        {
            sw.Restart();
            fileStream.Seek(offsetBytes, SeekOrigin.Begin);
            fileStream.Read(buffer, 0, blockSize);
            //if (flushBuf) fileStream.Flush();
            sw.Stop();
        }

        protected override byte[] InitBuffer()
        {
            return new byte[blockSize];
        }
    }
}