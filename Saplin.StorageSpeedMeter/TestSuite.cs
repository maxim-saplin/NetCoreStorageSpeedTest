using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Saplin.StorageSpeedMeter
{
    public abstract class TestSuite
    {
        protected List<Test> tests;
        Stopwatch sw = new Stopwatch();

        public event EventHandler<TestUpdateEventArgs> StatusUpdate;

        protected bool breakCalled = false;
        protected TestResults[] results;

        public TestSuite()
        {
            tests = new List<Test>();
        }

        public void AddTest(Test test)
        {
            tests.Add(test);
            test.StatusUpdate += (sender, e) => this.StatusUpdate?.Invoke(sender, e);
        }

        public void RemoveTest(Test test)
        {
            tests.Remove(test);
        }

        public IEnumerable<Test> ListTests()
        {
            return tests;
        }

        public void Break()
        {
            breakCalled = true;

            foreach (Test t in tests) t.Break();
        }

        protected virtual void PrerequisiteCleanup(int testIndex)
        {
            GC.Collect(2, GCCollectionMode.Forced, true);
        }

        private void ResetTests()
        {
            foreach (var t in tests)
                t.Status = TestStatus.NotStarted;

            breakCalled = false;
        }

        public virtual TestResults[] Execute()
        {
            ResetTests();

            results = new TestResults[tests.Count];
            completedTests = 0;

            sw.Restart();

            foreach (Test t in tests)
            {
                if (t.PrerequsiteCleanup)
                    PrerequisiteCleanup(completedTests - 1);

                var r = t.Execute();

                results[completedTests] = r;
                completedTests++;

                if (breakCalled) break;
            }

            sw.Stop();

            return results;
        }

        public void ExportToCsv(string folderPath, bool saveAllDataPoints)
        {
            const char separator = ';';
            const string aggregateFile = "Aggrgate-Results-{0:s}.csv";
            const string rawResultsFile = "Raw-Results-{0}-{1:s}.csv";

            if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);

            var now = DateTime.Now;

            var fileName = string.Format(aggregateFile, now);
            fileName = Path.GetInvalidFileNameChars().Aggregate(fileName, (current, c) => current.Replace(c.ToString(), string.Empty));

            SaveAggregateResults(folderPath, fileName, separator, now);

            if (saveAllDataPoints)
            {
                foreach (var r in results)
                {
                    if (r != null)
                    {
                        fileName = string.Format(rawResultsFile, r.TestName, now);
                        fileName = Path.GetInvalidFileNameChars().Aggregate(fileName, (current, c) => current.Replace(c.ToString(), string.Empty));

                        SaveRawResults(folderPath, fileName, separator, r);
                    }
                }
            }
        }

        private void SaveAggregateResults(string folderPath, string fileName, char separator, DateTime now)
        {
            var stream = System.IO.File.CreateText(
                Path.Combine(
                    folderPath,
                    fileName
                    ));

            using (stream)
            {
                stream.WriteLine(
                    "Test{0}Drive{0}DateTime{0}Average(Cache Normalized)[Mb/s]{0}Average[Mb/s]{0}Mean[Mb/s]{0}Min[Mb/s]{0}Max[Mb/s]{0}Duration[s]{0}Block[B]{0}Traffic[Mb]{0}OS{0}Machine{0}", separator
                    );

                foreach (var r in results)
                {
                    if (r != null)
                        stream.WriteLine("{1}{0}{2}{0}{3:s}{0}{4}{0}{5}{0}{6}{0}{7}{0}{8}{0}{9}{0}{10}{0}{11}{0}{12}{0}{13}{0}",
                            separator,
                            r.TestName,
                            Directory.GetDirectoryRoot(folderPath),
                            now,
                            r.AvgThoughputNormalized,
                            r.AvgThoughput,
                            r.Mean,
                            r.Min,
                            r.Max,
                            r.TotalTimeMs / 1000,
                            r.BlockSize,
                            r.TotalTraffic / 1024 / 1024,
                            Environment.OSVersion,
                            Environment.MachineName
                            );
                }

                stream.WriteLine("Write score(MB/s):{0}{1}{0}Read score(MB/s):{0}{2}{0}", separator, WriteScore, ReadScore);
            }
        }

        private static void SaveRawResults(string folderPath, string fileName, char separator, TestResults r)
        {
            var stream = System.IO.File.CreateText(
            Path.Combine(
                folderPath,
                fileName
                ));

            using (stream)
            {
                if (!r.HasPositions)
                {
                    stream.WriteLine("Throughput(Mb/s){0}", separator);

                    foreach (var r0 in r as IEnumerable<double>)
                    {
                        stream.WriteLine("{1}{0}", separator, r0);
                    }
                }
                else
                {
                    stream.WriteLine("Throughput(Mb/s){0}Block start position(byte index){0}", separator);

                    foreach (var r0 in r as IEnumerable<Tuple<double, long>>)
                    {
                        stream.WriteLine("{1}{0}{2}{0}", separator, r0.Item1, r0.Item2);
                    }
                }

            }
        }


        public virtual long ElapsedMs
        {
            get
            {
                return sw.ElapsedMilliseconds;
            }
        }

        public virtual long RemainingMs
        {
            get
            {
                return -1;
            }
        }

        public int TotalTests
        {
            get
            {
                return tests.Count;
            }
        }

        int completedTests;

        public int CompletedTests
        {
            get
            {
                return completedTests;
            }
        }

        public abstract double WriteScore
        {
            get;
        }

        public abstract double ReadScore
        {
            get;
        }
    }
}
