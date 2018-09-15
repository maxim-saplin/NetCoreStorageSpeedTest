using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
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
        protected List<TestResults> results;

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

        /// <summary>
        /// If calling from different thread (not within EventHandler within threaed used to run Execute method), you should wait for Execute method to return
        /// </summary>
        public virtual void Break()
        {
            breakCalled = true;

            foreach (Test t in tests) t.Break();
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

            results = new List<TestResults>();
            completedTests = 0;

            sw.Restart();

            foreach (Test t in tests)
            {
                var r = t.Execute();

                results.Add(r);
                completedTests++;

                if (breakCalled) break;
            }

            sw.Stop();

            return results.ToArray();
        }

        public string ExportToCsv(string folderPath, bool saveAllDataPoints, DateTime? dateTime = null, string separator = ";", string decimalSeparator = ",")
        {
            const string aggregateFile = "Aggrgate-Results-{0:s}.csv";
            const string rawResultsFile = "Raw-Results-{0}-{1:s}.csv";

            if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);

            var now = dateTime.HasValue ? dateTime.Value : DateTime.Now;

            var fileName = string.Format(aggregateFile, now);
            fileName = Path.GetInvalidFileNameChars().Aggregate(fileName, (current, c) => current.Replace(c.ToString(), string.Empty));

            SaveAggregateResults(folderPath, fileName, separator, decimalSeparator, now);

            if (saveAllDataPoints)
            {
                foreach (var r in results)
                {
                    if (r != null)
                    {
                        fileName = string.Format(rawResultsFile, r.TestDisplayName, now);
                        fileName = Path.GetInvalidFileNameChars().Aggregate(fileName, (current, c) => current.Replace(c.ToString(), string.Empty));

                        SaveRawResults(folderPath, fileName, separator, decimalSeparator, r);
                    }
                }
            }

            return fileName;
        }

        private void SaveAggregateResults(string folderPath, string fileName, string separator, string decimalSeparator, DateTime now)
        {
            var nfi = new NumberFormatInfo
            {
                NumberDecimalSeparator = decimalSeparator,
                NumberGroupSeparator = String.Empty
            };

            var stream = System.IO.File.CreateText(
                Path.Combine(
                    folderPath,
                    fileName
                    ));

            using (stream)
            {
                stream.WriteLine("sep=" + separator);
                stream.WriteLine(
                    "Test{0}Drive{0}DateTime{0}Average[Mb/s]{0}Mean[Mb/s]{0}Min[Mb/s]{0}Max[Mb/s]{0}Duration[s]{0}Block[B]{0}Traffic[Mb]{0}OS{0}Machine{0}", separator
                    );

                foreach (var r in results)
                {
                    if (r != null)
                    {
                        var s = string.Format(nfi, "{1}{0}{2}{0}{3:s}{0}{5:N}{0}{6:N}{0}{7:N}{0}{8:N}{0}{9}{0}{10}{0}{11}{0}{12}{0}{13}{0}",
                            separator,                                  //{0}
                            r.TestDisplayName,                          //{1}
                            Directory.GetDirectoryRoot(folderPath),     //{2}
                            now,                                        //{3}
                            r.AvgThroughputNormalized,                  //{4}
                            r.AvgThroughput,                            //{5}
                            r.Mean,                                     //{6}
                            r.Min,                                      //{7}
                            r.Max,                                      //{8}
                            r.TotalTimeMs / 1000,                       //{9}
                            r.BlockSizeBytes,                           //{10}
                            r.TotalTraffic / 1024 / 1024,               //{11}
                            Environment.OSVersion,                      //{12}
                            Environment.MachineName                     //{12}
                            );

                        stream.WriteLine(s);
                    }
                }

                stream.WriteLine("Write score(MB/s):{0}{1}{0}Read score(MB/s):{0}{2}{0}", separator, WriteScore.ToString(nfi), ReadScore.ToString(nfi));
            }
        }

        private static void SaveRawResults(string folderPath, string fileName, string separator, string decimalSeparator, TestResults r)
        {
            var nfi = new NumberFormatInfo
            {
                NumberDecimalSeparator = decimalSeparator,
                NumberGroupSeparator = String.Empty
            };

            var stream = System.IO.File.CreateText(
            Path.Combine(
                folderPath,
                fileName
                ));

            using (stream)
            {
                stream.WriteLine("sep=" + separator);

                if (!r.HasPositions)
                {
                    stream.WriteLine("Throughput(Mb/s){0}", separator);

                    foreach (var r0 in r as IEnumerable<double>)
                    {
                        stream.WriteLine("{1}{0}", separator, r0.ToString(nfi));
                    }
                }
                else
                {
                    stream.WriteLine("Throughput(Mb/s){0}Block start position(byte index){0}", separator);

                    foreach (var r0 in r as IEnumerable<Tuple<double, long>>)
                    {
                        stream.WriteLine("{1}{0}{2}{0}", separator, r0.Item1.ToString(nfi), r0.Item2);
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
