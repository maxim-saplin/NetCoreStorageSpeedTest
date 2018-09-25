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

            minBlock = 0;
            maxBlock = (fileSize / blockSize) - 1;

            //maxBlock = (fileSize / 2 / blockSize)-1;
            //minBlock = 0;
        }

        protected override void DoOperation(byte[] data, Stopwatch sw, long currBlock, int i)
        {
            byte b;
            sw.Restart();
            fileStream.Seek(currBlock, SeekOrigin.Begin);
            fileStream.Read(data, 0, blockSize);
            //for (int k = 0; k < blockSize; k += 4)
                //b = data[k];
            //foreach(var b in data) // Android seems to be to much clever and not iterating throug values seems to be working like mapping file to array with no read
            //{}
            sw.Stop();
        }

        protected override byte[] InitBuffer()
        {
            return new byte[blockSize];
        }
    }
}