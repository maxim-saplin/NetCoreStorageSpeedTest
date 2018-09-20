using System.Diagnostics;
using System.IO;

namespace Saplin.StorageSpeedMeter
{
    public class RandomReadTest : RandomTest
    {
        private bool flushBuf = false;

        public RandomReadTest(TestFile file, int blockSize, int testTimeSecs = 30) : base(file.ReadStream, blockSize, testTimeSecs)
        {
            flushBuf = file.enableMemCache;
        }

        public override string DisplayName { get => "Random read" + " [" + blockSize / 1024 + "KB] block"; }

        protected override void ValidateAndInitParams()
        {
            base.ValidateAndInitParams();

            // Decided not to use different ranges for read and write random tests since fore Mechanical drives this can be an issue (different travel ranges and different seeks times)
            //maxBlock = file.Length / blockSize/2 - 1; // only half of the file is used. Practise showed, that OS can store in RAM significant portition of previously written/read file (by previous tests). Tests in W10 with 16Gb of RAM showed caching of up to 8Gb
            //minBlock = 0; 
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