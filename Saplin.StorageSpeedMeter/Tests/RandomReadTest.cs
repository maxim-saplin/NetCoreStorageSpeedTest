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
            if (cachePurger != null) IsNormalizedAvg = true;
            fileSize = file.TestAreaSizeBytes;
        }

        public override string DisplayName { get => "Random read" + " [" + blockSize / 1024 + "KB] block"; }

        protected override void ValidateAndInitParams()
        {
            base.ValidateAndInitParams();

            minBlock = 0;
            maxBlock = (fileSize / blockSize) - 1;

            //maxBlock = (fileSize / 2 / blockSize)-1;
            //minBlock = 0;
        }

        protected override void DoOperation(byte[] data, Stopwatch sw, long currBlock, int i)
        {
            sw.Restart();
            fileStream.Seek(currBlock, SeekOrigin.Begin);
            fileStream.Read(data, 0, blockSize);
            sw.Stop();
        }

        protected override byte[] InitBuffer()
        {
            return new byte[blockSize];
        }
    }
}