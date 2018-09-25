using System;
using System.Diagnostics;
using System.IO;

namespace Saplin.StorageSpeedMeter
{
    public class RandomWriteTest : RandomTest
    {
        private bool flushBuf = false;
        private long fileSize;

        public RandomWriteTest(TestFile file, int blockSize, int testTimeSecs = 30) : base(file.WriteStream, blockSize, testTimeSecs)
        {
            flushBuf = !file.writeBuffering;
            fileSize = file.TestAreaSizeBytes;
        }

        public override string DisplayName { get => "Random write" + " [" + blockSize / 1024 + "KB] block"; }

        protected override void ValidateAndInitParams()
        {
            base.ValidateAndInitParams();

            minBlock = 0;
            maxBlock = (fileSize / blockSize) - 1;

            //minBlock = fileSize / blockSize / 2;
            //maxBlock = fileSize / blockSize - 1; // only half of the file is used to help deal with mem caching 
        }

        Random rand;

        protected override void DoOperation(byte[] data, Stopwatch sw, long currBlock, int i)
        {
            data[rand.Next(data.Length)] = (byte)rand.Next(); //change the block, JIC there's some clever cachning noticing same block is being writen
            sw.Restart();
            fileStream.Seek(currBlock, SeekOrigin.Begin);
            fileStream.Write(data, i % blocksInMemory, blockSize);
            if (flushBuf) fileStream.Flush(true);
            sw.Stop();
        }

        protected override byte[] InitBuffer()
        {
            Status = TestStatus.InitMemBuffer;

            rand = new Random();
            var data = new byte[blockSize * blocksInMemory];
            rand.NextBytes(data);
            return data;
        }
    }
}