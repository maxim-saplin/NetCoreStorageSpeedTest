using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Saplin.StorageSpeedMeter
{
    public class BigTest : TestSuite, IDisposable
    {
        const long fileSize = 1024 * 1024 * 1024;
        public const int bigBlockSize = 4 * 1024 * 1024;
        public const int smallBlockSize = 4 * 1024;
        const double readFileToFullRatio = 1.0; // sequential read is executed only on a portion of file
        const double avgReadToWriteRatio = 1.1; // starting point for elapsed time estimation
        const int randomDuration = 20;

        long bigBlocksNumber;

        TestFile file;

        private const long maxArraySize = 128 * 1024 * 1024; // 0.5Gb

        public BigTest(string drivePath)
        {

            file = new TestFile(drivePath);
            bigBlocksNumber = fileSize / bigBlockSize;

            AddTest(new SequentialWriteTest(file.WriteStream, bigBlockSize, bigBlocksNumber, true));
            AddTest(new SequentialReadTest(file.ReadStream, bigBlockSize, (long)(bigBlocksNumber * readFileToFullRatio)));
            AddTest(new RandomWriteTest(file.WriteStream, smallBlockSize, randomDuration));
            AddTest(new RandomReadTest(file.ReadStream, smallBlockSize, randomDuration));

            SetUpRemainigCalculations();

            AddTest(new MemCopyTest(file.ReadStream/*hack, memcopy doesn't need file*/, 128 * 1024 * 1024, 12));
        }

        long remainingMs;
        long remainingSeqWriteMs;
        long remainingSeqReadMs;
        long remainingRandomMs;
        long remainingCleanupMs;
        double avgWriteSpeedMbs = 100; //MB/s
        double avgReadSpeedMbs = 110; //MB/s

        private void SetUpRemainigCalculations()
        {
            long readFileSize = (long)(readFileToFullRatio * fileSize);

            remainingSeqWriteMs = (long)(fileSize / (avgWriteSpeedMbs * 1024 * 1024) * 1000);
            remainingSeqReadMs = (long)(readFileSize / (avgReadSpeedMbs * 1024 * 1024) * 1000);
            remainingRandomMs = ListTests().Where(t => t.GetType().BaseType == typeof(RandomTest)).Count() * randomDuration * 1000;

            var cleanups = tests.Count<Test>(t => t.PrerequsiteCleanup);
            remainingCleanupMs = cleanups * cleanupTimeMs;

            var remainingRandomBaseMs = remainingRandomMs;

            remainingMs = remainingSeqWriteMs + remainingRandomMs + remainingSeqReadMs + remainingCleanupMs;

            long seqReadElapsedStart = -1;
            var seqReadSpeedUpdated = false;

            object prevTest = null;

            this.StatusUpdate += (sender, e) =>
            {
                //SequentialWriteTest
                if (sender.GetType() == typeof(SequentialWriteTest))
                {
                    if (e.Status == TestStatus.Completed)
                    {
                        seqReadElapsedStart = ElapsedMs;
                        remainingSeqReadMs = 0;
                    }
                    else if (e.ProgressPercent != null)
                    {
                        remainingSeqWriteMs = (long)((100 - e.ProgressPercent) / (e.ProgressPercent / ElapsedMs));

                        if (e.ProgressPercent > 5 && !seqReadSpeedUpdated)
                        {
                            avgWriteSpeedMbs = (double)(((double)fileSize / 1024 / 1024) * (e.ProgressPercent / 100) / (ElapsedMs / 1000));
                            avgReadSpeedMbs = avgReadToWriteRatio * avgWriteSpeedMbs;
                            remainingSeqReadMs = (long)(readFileSize / (avgReadSpeedMbs * 1024 * 1024) * 1000);
                            seqReadSpeedUpdated = true;
                        }
                    }
                }
                //RandomTest
                else if (sender.GetType().BaseType == typeof(RandomTest))
                {
                    if (prevTest != sender)
                    {
                        remainingRandomBaseMs = remainingRandomBaseMs - randomDuration * 1000;
                        prevTest = sender;

                        if ((sender as Test).PrerequsiteCleanup)
                        {
                            cleanups--;
                            remainingCleanupMs = cleanups * cleanupTimeMs;
                        }
                    }
                    if (e.Status == TestStatus.Completed)
                    {
                        seqReadElapsedStart = ElapsedMs;
                        remainingRandomMs = remainingRandomBaseMs;
                    }
                    else if (e.ProgressPercent != null)
                    {
                        remainingRandomMs = (long)(remainingRandomBaseMs + randomDuration * 1000 * (1 - e.ProgressPercent / 100));
                    }
                }
                //SequentialReadTest - SeqRead is assumed to follow SeqWrite
                else if (sender.GetType() == typeof(SequentialReadTest))
                {
                    if (e.Status == TestStatus.Completed)
                    {
                        remainingSeqReadMs = 0;
                    }
                    else if (e.ProgressPercent != null && e.ProgressPercent > 10)
                        remainingSeqReadMs = (long)((100 - e.ProgressPercent) / (e.ProgressPercent / (ElapsedMs - seqReadElapsedStart)));
                }

                remainingMs = Math.Max(0, remainingSeqWriteMs + remainingRandomMs + remainingSeqReadMs + remainingCleanupMs);
            };
        }

        public override TestResults[] Execute()
        {
            writeScore = readScore = null;

            var results = base.Execute();

            file.Dispose();

            return results;
        }

        private long cleanupTimeMs = 1;

        protected override void PrerequisiteCleanup(int testIndex)
        {
        }

        public override long RemainingMs => remainingMs;

        public string FilePath
        {
            get
            {
                return file.Path;
            }
        }

        public string FileFolderPath
        {
            get
            {
                return file.FolderPath;
            }
        }

        public string ResultsFolderPath
        {
            get
            {
                return System.IO.Path.Combine(file.FolderPath, "StorageSpeedTestResults");
            }
        }

        double? writeScore;
        double? readScore;

        /// <summary>
        /// "Megabytes per second" balanced write score which composes random and sequential write speed. 
        /// </summary>
        /// <remarks>
        /// The general rule for calculating this score is determining avergage write speed, where 80% of total traffic comes from sequential write and another 
        /// 20% of trafics comes from random writes. In the default case, a test suite contains 1 sequential write test and 4 random write tests 
        /// (blocks of 4, 8, 64 and 256 Kb). In this case, calculation assumes 80% traffics coming from sequqntial operation (at the speed of coresponding test)
        /// and 5% from each of random write series at speeds, corresponding to measured speeds.
        /// Calculation. Assume 1Gb file is written sequentially at 0.1 Gb/s (10s). After a number of smaller blocks of size 4Kb are written on top
        /// of existing data at random positions within the original 1Gb, ammounting to a total of 0.5Gb. If average speed of writes is 0,01Gb/s,  the
        /// time to complete these random writes at 0.5/0.01=50 seconds. Thus far it took 10+50 = 60 seconds to write 1.5Gb to disk. Then there's another
        /// attempt to do writes of total size of 0.5Gb at 0.05Gb/s taking 10s. This gives us an total time of 70 seconds writing 2Gb to disk. Average speed 
        /// in this case is 2Gb/70s = 0.029Gb/s. A perfect drive will have little differences between rnadom writes of small blocks and sequential writes which
        /// will lead to maximizing score. On the other hand in reality random writes is chalenging for many drives (especially HDDs) and while sequential
        /// writes (e.g. video processing) can be fast, random writes (e.g. database writes, working with multiple files) demonstrate 10X and greater
        /// performance decline over sequntial operations.
        /// </remarks>
        public override double WriteScore
        {
            get
            {
                if (writeScore == null)
                {
                    ValidateTestsForScores();

                    var sequentialTest = typeof(SequentialWriteTest);
                    var randomTest = typeof(RandomWriteTest);

                    writeScore = CalculateScore(sequentialTest, randomTest);
                }

                return writeScore.Value;
            }
        }

        /// <summary>
        /// "Megabytes per second" balanced read score which composes random and sequential read speed. 
        /// </summary>
        /// <remarks>
        /// The general rule for calculating this score is determining avergage write speed, where 80% of total traffic comes from sequential write and another 
        /// 20% of trafics comes from random writes. In the default case, a test suite contains 1 sequential write test and 4 random write tests 
        /// (blocks of 4, 8, 64 and 256 Kb). In this case, calculation assumes 80% traffics coming from sequqntial operation (at the speed of coresponding test)
        /// and 5% from each of random write series at speeds, corresponding to measured speeds.
        /// Calculation. Assume 1Gb file is written sequentially at 0.1 Gb/s (10s). After a number of smaller blocks of size 4Kb are written on top
        /// of existing data at random positions within the original 1Gb, ammounting to a total of 0.5Gb. If average speed of writes is 0,01Gb/s,  the
        /// time to complete these random writes at 0.5/0.01=50 seconds. Thus far it took 10+50 = 60 seconds to write 1.5Gb to disk. Then there's another
        /// attempt to do writes of total size of 0.5Gb at 0.05Gb/s taking 10s. This gives us an total time of 70 seconds writing 2Gb to disk. Average speed 
        /// in this case is 2Gb/70s = 0.029Gb/s. A perfect drive will have little differences between rnadom writes of small blocks and sequential writes which
        /// will lead to maximizing score. On the other hand in reality random writes is chalenging for many drives (especially HDDs) and while sequential
        /// writes (e.g. video processing) can be fast, random writes (e.g. database writes, working with multiple files) demonstrate 10X and greater
        /// performance decline over sequntial operations.
        /// </remarks>
        public override double ReadScore
        {
            get
            {
                if (readScore == null)
                {
                    ValidateTestsForScores();

                    var sequentialTest = typeof(SequentialReadTest);
                    var randomTest = typeof(RandomReadTest);

                    readScore = CalculateScore(sequentialTest, randomTest);
                }

                return readScore.Value;
            }
        }

        public long FileSize
        {
            get
            {
                return fileSize;
            }
        }

        public static long FreeSpaceRequired
        {
            get
            {
                return (long)(RamDiskUtil.TotalRam * 1.0);
            }
        }

        private double CalculateScore(Type sequentialTest, Type randomTest)
        {
            var timesAndSizes = new List<Tuple<double, double>>(); //seconds and megabytes
            double throughput;
            const double sizeSeq = 800;
            const double sizeRand = 200;
            var randomTests = tests.Count<Test>(t => t.GetType() == randomTest);

            for (var i = 0; i < tests.Count; i++)
            {
                if (tests[i].GetType() == sequentialTest)
                {
                    throughput = results[i].AvgThoughputNormalized;
                    timesAndSizes.Add(new Tuple<double, double>(sizeSeq / throughput, sizeSeq));
                }
                else if (tests[i].GetType() == randomTest)
                {
                    throughput = results[i].AvgThoughputNormalized;
                    timesAndSizes.Add(new Tuple<double, double>(sizeRand / randomTests / throughput, sizeRand / randomTests));
                }
            }

            var time = timesAndSizes.Sum<Tuple<double, double>>(t => t.Item1);
            var dataSize = timesAndSizes.Sum<Tuple<double, double>>(t => t.Item2);

            return dataSize / time;
        }

        private void ValidateTestsForScores()
        {
            if (tests.Any<Test>(t => t.Status != TestStatus.Completed)) throw new InvalidOperationException("Cant't calculate agregate score on a test suite with not all tests completed");
        }

        public void Dispose()
        {
            Dispose(true);

            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                file?.Dispose();
            }
        }
    }
}
