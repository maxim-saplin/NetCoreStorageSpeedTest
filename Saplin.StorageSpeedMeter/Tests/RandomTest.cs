﻿using System;
using System.Diagnostics;
using System.IO;

namespace Saplin.StorageSpeedMeter
{
    public abstract class RandomTest : Test
    {
        protected const int memoryBuffSize = 128 * 1024 * 1024;
        private const int maxBlocksInTest = 1 * 1024 * 1024; // 8 Mb for shuffled positions

        protected readonly FileStream file;
        protected readonly int blockSize;
        protected readonly double? percentOfFileToOwerwrite;
        protected readonly int? maxTestTime;
        protected readonly int blocksInMemory;

        protected long maxBlock; // max index of block accesible
        protected long minBlock;

        public override string Name { get => "Random write" + " [" + blockSize / 1024 + "Kb] block"; }

        public RandomTest(FileStream file, int blockSize, int maxTestTimeSecs = 25)
        {
            if (percentOfFileToOwerwrite <= 0) throw new ArgumentOutOfRangeException("percentOfFileToOwerwrite", "Size of file to be overwritten (in percents of it's current size) must greater than 0");
            if (blockSize <= 0) throw new ArgumentOutOfRangeException("blockSize", "Block size cant be negative");

            this.file = file;
            this.blockSize = blockSize;
            blocksInMemory = memoryBuffSize / blockSize;
            this.maxTestTime = maxTestTimeSecs;
        }

        private long[] positionsPlan;

        protected virtual void ValidateAndInitParams()
        {
            if (file.Length == 0) throw new InvalidOperationException("File can't be empty");
            if (blockSize > file.Length) throw new InvalidOperationException("Block size cant be greater than file size");

            maxBlock = (file.Length /2 / blockSize);
            minBlock = 0;
        }

        private void GeneratePositionsPlan() // to avoid RAM caching, it's important to avoid repetative reads of same blocks - if read once, the block might stay in RAM cache and next read will yield RAM value. Possition shuffaling is used
        {
            positionsPlan = new long[Math.Min(maxBlock - minBlock, maxBlocksInTest)];
            for (long i = 0; i < positionsPlan.Length - 1; i++)
                positionsPlan[i] = i*blockSize;
            Shuffle(positionsPlan);
        }

        private void Shuffle(long[] list)
        {
            var rng = new Random();
            var n = list.Length;

            while (n > 1)
            {
                n--;
                int k = rng.Next(n + 1);
                long value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }

        public override TestResults Execute()
        {
            ValidateAndInitParams();
            GeneratePositionsPlan();

            status = TestStatus.Started;

            byte[] data = InitDataArray();

            HelloWorld();

            var sw = new Stopwatch();
            var results = new TestResults(Test.unitMbs, Name, blockSize);

            int prevPercent = -1;
            int curPercent = -1;
            long currOffset = 0;

            var i = 0;

            var elapsed = new Stopwatch();
            elapsed.Start();

            while (i < positionsPlan.Length + 1)
            {
                //currBlock = rand.NextLong(minBlock, maxBlock +1) * blockSize;
                currOffset = positionsPlan[i];

                i++; // easier to calculate progress in precent when starting with 1

                DoOperation(data, sw, currOffset, i);

                results.AddTroughputMbs(blockSize, currOffset, sw);

                if (breakCalled)
                {
                    Update("Test interrupted");
                    return results;
                }

                curPercent = Math.Min(
                    100, 
                    Math.Max(
                        (int)(elapsed.ElapsedMilliseconds / 10 / maxTestTime), 
                        curPercent = (int)(i * 100 / (double)positionsPlan.Length)
                        )
                );

                if (curPercent > prevPercent)
                {
                    Update(string.Format("{0:0.00}{1}", results.GetLatest5MeanResult(), results.Unit), curPercent);
                    prevPercent = curPercent;

                    if (curPercent == 100) break;
                }
            }

            elapsed.Stop();
            results.TotalTimeMs = elapsed.ElapsedMilliseconds;

            status = TestStatus.Completed;

            FinalUpdate(results, elapsed.ElapsedMilliseconds);

            return results;
        }

        protected abstract void HelloWorld();

        protected abstract void DoOperation(byte[] data, Stopwatch sw, long currBlock, int i);

        protected abstract byte[] InitDataArray();
    }
}

