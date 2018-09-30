using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Saplin.StorageSpeedMeter
{
    public enum MemCacheOptions
    {
        Enabled, 
        Disabled, // Windows and OS
        DisabledEmulation // Other OS, try trick system into purging cache
    }

    public class BigTest : TestSuite, IDisposable
    {
        public readonly long fileSize;
        public const int bigBlockSize = 4 * 1024 * 1024;
        public const int smallBlockSize = 4 * 1024;
        public const int mediumBlockSize = 32 * 1024;
        const double readFileToFullRatio = 1.0; // sequential read can be executed only on a portion of file
        const double avgReadToWriteRatio = 1.1; // starting point for elapsed time estimation
        const int randomTestDuration = 7;

        TestFile file;

        private const long maxArraySize = 128 * 1024 * 1024;

        /// <summary>
        /// Test suite of 2 sequential and 2 random tests
        /// </summary>
        /// <param name="drivePath">Drive name</param>
        /// <param name="fileSize">Test file size, default is 1Gb</param>
        /// <param name="writeBuffering">Faster writes through buffering</param>
        /// <param name="memCache">Faster reads through File Cache</param>
        /// <param name="filePath">Ignore drivepath, do not use auto file name generation and use absolute path to the file</param>
        /// <param name="freeMem">Delegate that gives info about free memory, e.g. under Android when .NET Standard doesn't have the faciclity</param>
        public BigTest(string drivePath, long fileSize = 1024 * 1024 * 1024, bool writeBuffering = false, MemCacheOptions memCache = MemCacheOptions.Disabled, string filePath = null, Func<long> freeMem = null)
        {
            Func<bool> checkBreakCalled = () => breakCalled;

            file = new TestFile(drivePath, fileSize, writeBuffering, memCache != MemCacheOptions.Disabled, filePath); // macOS and Windows mem cahce can be dissabled at OS level for specifc file handles, no such options found for Android
            this.fileSize = fileSize;

            AddTest(new SequentialWriteTest(file, bigBlockSize, true));
            AddTest(new SequentialReadTest(file, bigBlockSize, memCache == MemCacheOptions.DisabledEmulation ? new CachePurger(file, freeMem, checkBreakCalled) : null));

            AddTest(new RandomWriteTest(file, smallBlockSize, randomTestDuration));
            AddTest(new RandomWriteTest(file, mediumBlockSize, randomTestDuration));

            AddTest(new RandomReadTest(file, smallBlockSize, randomTestDuration, memCache == MemCacheOptions.DisabledEmulation ? new CachePurger(file, freeMem, checkBreakCalled) : null));
            AddTest(new RandomReadTest(file, mediumBlockSize, randomTestDuration, memCache == MemCacheOptions.DisabledEmulation ? new CachePurger(file, freeMem, checkBreakCalled) : null));

            SetUpRemainigCalculations();

            AddTest(new MemCopyTest(bigBlockSize, 8192, freeMem));
        }

        long remainingMs;
        long remainingSeqWriteMs;
        long remainingSeqReadMs;
        long remainingRandomMs;
        double avgWriteSpeedMbs = 100; //MB/s
        double avgReadSpeedMbs = 110; //MB/s

        private void SetUpRemainigCalculations()
        {
            long readFileSize = (long)(readFileToFullRatio * fileSize);

            remainingSeqWriteMs = (long)(fileSize / (avgWriteSpeedMbs * 1024 * 1024) * 1000);
            remainingSeqReadMs = (long)(readFileSize / (avgReadSpeedMbs * 1024 * 1024) * 1000);
            remainingRandomMs = ListTests().Where(t => t.GetType().BaseType == typeof(RandomTest)).Count() * randomTestDuration * 1000;

            var remainingRandomBaseMs = remainingRandomMs;

            remainingMs = remainingSeqWriteMs + remainingRandomMs + remainingSeqReadMs;

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
                        remainingRandomBaseMs = remainingRandomBaseMs - randomTestDuration * 1000;
                        prevTest = sender;

                    }
                    if (e.Status == TestStatus.Completed)
                    {
                        seqReadElapsedStart = ElapsedMs;
                        remainingRandomMs = remainingRandomBaseMs;
                    }
                    else if (e.ProgressPercent != null)
                    {
                        remainingRandomMs = (long)(remainingRandomBaseMs + randomTestDuration * 1000 * (1 - e.ProgressPercent / 100));
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

                remainingMs = Math.Max(0, remainingSeqWriteMs + remainingRandomMs + remainingSeqReadMs);
            };
        }

        public override TestResults[] Execute()
        {
            writeScore = readScore = null;

            var results = base.Execute();

            return results;
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
                    throughput = results[i].AvgThroughputNormalized;
                    timesAndSizes.Add(new Tuple<double, double>(sizeSeq / throughput, sizeSeq));
                }
                else if (tests[i].GetType() == randomTest)
                {
                    throughput = results[i].AvgThroughputNormalized;
                    timesAndSizes.Add(new Tuple<double, double>(sizeRand / randomTests / throughput, sizeRand / randomTests));
                }
            }

            var time = timesAndSizes.Sum<Tuple<double, double>>(t => t.Item1);
            var dataSize = timesAndSizes.Sum<Tuple<double, double>>(t => t.Item2);

            return dataSize / time;
        }

        private void ValidateTestsForScores()
        {
            if (tests.Cast<Test>().Where(t => t.GetType() != typeof(MemCopyTest)).Any(t => t.Status != TestStatus.Completed)) throw new InvalidOperationException("Cant't calculate agregate score on a test suite with not all tests completed");
        }

        public void Dispose()
        {
            Dispose(true);
            tests = null;
            GC.Collect(2, GCCollectionMode.Forced, true);

            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                file?.Dispose();
            }
        }

        ~BigTest()
        {
            Dispose(false);
        }
    }
}
