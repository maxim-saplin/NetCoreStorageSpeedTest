using System;
using System.Diagnostics;
using System.IO;

namespace Saplin.StorageSpeedMeter
{
    public class RandomWriteTest : RandomTest
    {
        public RandomWriteTest(FileStream file, int blockSize, int testTimeSecs = 30) : base(file, blockSize, testTimeSecs)
        {
        }

        protected override void HelloWorld()
        {
            Update(string.Format("Executing random write test, [{0}Kb] block", blockSize / 1024));
        }

        Random rand;

        protected override void DoOperation(byte[] data, Stopwatch sw, long currBlock, int i)
        {
            data[rand.Next(data.Length)] = (byte)rand.Next(); //change to the block, JIC there's some clever cachning noticing same block is being writen
            sw.Restart();
            file.Seek(currBlock, SeekOrigin.Begin);
            file.Write(data, i % blocksInMemory, blockSize);
            file.Flush();
            sw.Stop();
        }

        protected override byte[] InitBuffer()
        {
            Update("Initilizing data in memory");

            rand = new Random();
            var data = new byte[blockSize * blocksInMemory];
            rand.NextBytes(data);
            return data;
        }
    }
}