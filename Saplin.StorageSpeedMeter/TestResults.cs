using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Saplin.StorageSpeedMeter
{
    public class TestResults : IEnumerable<double>, IEnumerable<Tuple<double, long>>
    {
        private int recalcCount = -1;
        private double min, minN, max, maxN, mean, avgThoughput, avgThoughputNormalized;
        private long totalTimeMs;

        public long TotalTimeMs
        {
            get { return totalTimeMs; }
            protected internal set { totalTimeMs = value; }
        }

        public double Min
        {
            get
            {
                Recalculate();

                return min;
            }
            protected set
            {
                min = value;
            }
        }

        public double Max
        {
            get
            {
                Recalculate();

                return max;
            }
            protected set
            {
                max = value;
            }
        }

        public double MinN
        {
            get
            {
                Recalculate();

                return minN;
            }
            protected set
            {
                minN = value;
            }
        }

        public double MaxN
        {
            get
            {
                Recalculate();

                return maxN;
            }
            protected set
            {
                maxN = value;
            }
        }

        public double Mean
        {
            get
            {
                Recalculate();

                return mean;
            }
            protected set
            {
                mean = value;
            }
        }

        /// <summary>
        /// // Average throughput is not equal to mean of all thoughput measure and is calculated assuming that average equals to Total Traffic over Total Time
        /// </summary>
        public double AvgThoughput
        {
            get
            {
                Recalculate();

                return avgThoughput;
            }
            protected set
            {
                avgThoughput = value;
            }
        }

        /// <summary>
        /// Throughput normalization assumes that some of the read measurements may come from RAM cache, rather than storage device, 
        /// and thus show speed of RAM, rather than storage device. After investigating histograms of test results (HDD, SSD), significant
        /// number of measurements where 20 - 200x higher than average. As a rule of thumb, the following conditions rule out  cached resuls:
        /// Max is 24x or more times higher than average, only values that are below 1.1 average are used for normalized trhoughput calculation
        /// </summary>
        public double AvgThoughputNormalized
        {
            get
            {
                Recalculate();

                return avgThoughputNormalized;
            }
            protected set
            {
                avgThoughputNormalized = value;
            }
        }

        public long TotalTraffic
        {
            get
            {
                return BlockSize * results.Count;
            }
        }

        public bool HasPositions
        {
            get
            {
                return positions.Count == results.Count;
            }
        }

        const int intialCapacity = 300000; //enough to store results on 64k block reads within 16Gig file
        List<double> results;
        List<long> positions;
        public string Unit { get; }
        public string TestName { get; }
        public long BlockSize { get; }

        public TestResults(string unit, string testName, long blockSize)
        {
            results = new List<double>(intialCapacity);
            positions = new List<long>();
            Unit = unit;
            TestName = testName;
            BlockSize = blockSize;
        }

        private void Recalculate()
        {
            if (results.Count == 0) throw new InvalidOperationException("No test results to calculate aggregate");

            if (recalcCount != results.Count)
            {
                min = results.Min<double>(tr => tr);
                max = results.Max<double>(tr => tr);
                mean = results.Average<double>(tr => tr);

                double inverseThroughputs = 0;// results.Select<double, double>(r => 1 / r).Sum();
                double inverseNormThroughputs = 0;
                int inverseNormCount = 0;

                foreach (var r in results)
                    inverseThroughputs += 1 / r;

                avgThoughput = results.Count / inverseThroughputs; // AvgThougput = [TotalTrafic] / [TotalTime] ___OR___ [Number of thoughput measures] / SUM OF [1 / (Nth throughput measure)]

                if (avgThoughput < max / 20) // More actual for HDD 
                {
                    foreach (var r in results)
                        if (r < avgThoughput * 10)
                        {
                            inverseNormThroughputs += 1 / r;
                            inverseNormCount++;
                        }

                    avgThoughputNormalized = inverseNormCount / inverseNormThroughputs;
                }
                else avgThoughputNormalized = avgThoughput;

                recalcCount = results.Count;
            }
        }

        public void AddResult(double result)
        {
            if (positions.Count > 0) throw new InvalidOperationException("You can't call this method once overload AddResult(double result, long position) has been called");

            results.Add(result);
        }

        public void AddResult(double result, long position)
        {
            if (positions.Count != results.Count) throw new InvalidOperationException("You can't call this method once overload AddResult(double result) has been called");

            results.Add(result);
            positions.Add(position);
        }

        public void AddTroughputMbs(long bytes, Stopwatch stopwatch)
        {
            double nanosecs = (double)stopwatch.ElapsedTicks * 1000000000 / Stopwatch.Frequency;

            AddResult(((double)bytes / 1024 / 1024) / (double)(nanosecs) * 1000 * 1000 * 1000);
        }

        public void AddTroughputMbs(long bytes, long position, Stopwatch stopwatch)
        {
            double nanosecs = (double)stopwatch.ElapsedTicks * 1000000000 / Stopwatch.Frequency;

            AddResult(((double)bytes / 1024 / 1024) / (double)(nanosecs) * 1000 * 1000 * 1000, position);
        }

        public double GetLatestResult()
        {
            return results[results.Count - 1];
        }

        public double GetLatest5MeanResult()
        {
            double[] vals = new double[5];

            vals[0] = results.Count > 0 ? results[results.Count - 1] : 0;
            vals[1] = results.Count > 1 ? results[results.Count - 2] : vals[0];
            vals[2] = results.Count > 2 ? results[results.Count - 3] : vals[1];
            vals[3] = results.Count > 3 ? results[results.Count - 4] : vals[2];
            vals[4] = results.Count > 4 ? results[results.Count - 5] : vals[3];

            return (vals[0] + vals[1] + vals[2] + vals[3] + vals[4]) / 5;
        }

        IEnumerator<double> IEnumerable<double>.GetEnumerator()
        {
            return results.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return results.GetEnumerator();
        }

        IEnumerator<Tuple<double, long>> IEnumerable<Tuple<double, long>>.GetEnumerator()
        {
            if (results.Count != positions.Count)
                throw new InvalidOperationException("'Results' and corresponding 'Positions' collections do not have same number of elements and it's impossible to match them");

            return results.Zip(positions, (r, p) => new Tuple<double, long>(r, p)).GetEnumerator();
        }

    }
}
