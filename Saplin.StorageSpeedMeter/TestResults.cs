using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Saplin.StorageSpeedMeter
{
    /// <summary>
    /// All units are MB/s until stated differently
    /// </summary>
    public class TestResults : IEnumerable<double>, IEnumerable<Tuple<double, long>>
    { 
        private int recalcCount = -1;
        private double min, minN, max, maxN, mean, avgThroughput, avgThroughputNormalized;
        private long totalTimeMs;

        const int intialCapacity = 300000; //enough to store results on 64k block reads within 16Gig file
        List<double> results;
        List<long> positions;
        public string TestDisplayName { get; }
        public long BlockSizeBytes { get; }

        public TestResults(Test test)
        {
            results = new List<double>(intialCapacity);
            positions = new List<long>();
            TestDisplayName = test.DisplayName;
            BlockSizeBytes = test.BlockSizeBytes;
            TestName = test.Name;
            TestType = test.GetType();
        }

        public Type TestType { get; private set; }

        public string TestName { get; private set; }

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

        /// <summary>
        /// Normalized, excludes bottom 1% of values
        /// </summary>
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

        /// <summary>
        /// Normalized, excludes top 1% of values
        /// </summary>
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
        public double AvgThroughput
        {
            get
            {
                Recalculate();

                return avgThroughput;
            }
            protected set
            {
                avgThroughput = value;
            }
        }

        /// <summary>
        /// Throughput normalization assumes that some of the read measurements may come from RAM cache, rather than storage device, 
        /// and thus show speed of RAM, rather than storage device. After investigating histograms of test results (HDD, SSD), significant
        /// number of measurements where 20 - 200x higher than average. As a rule of thumb, the following conditions rule out  cached resuls:
        /// Max is 24x or more times higher than average, only values that are below 1.1 average are used for normalized trhoughput calculation
        /// </summary>
        public double AvgThroughputNormalized
        {
            get
            {
                Recalculate();

                return avgThroughputNormalized;
            }
            protected set
            {
                avgThroughputNormalized = value;
            }
        }

        public long TotalTraffic
        {
            get
            {
                return BlockSizeBytes * results.Count;
            }
        }

        public bool HasPositions
        {
            get
            {
                return positions.Count == results.Count;
            }
        }

        private void Recalculate()
        {
            if (results.Count == 0) return;

            if (recalcCount != results.Count)
            {
                results.Sort();
                min = results[0];
                max = results[results.Count - 1];
                minN = results[(int)(results.Count * .01)];
                maxN = results[(int)(results.Count * 0.99)];
                mean = results.Average<double>(tr => tr);

                double inverseThroughputs = 0;// results.Select<double, double>(r => 1 / r).Sum();
                double inverseNormThroughputs = 0;
                int inverseNormCount = 0;

                foreach (var r in results)
                    inverseThroughputs += 1 / r;

                avgThroughput = results.Count / inverseThroughputs; // AvgThougput = [TotalTrafic] / [TotalTime] ___OR___ [Number of thoughput measures] / SUM OF [1 / (Nth throughput measure)]

                if (avgThroughput < max / 20) // More actual for HDD 
                {
                    foreach (var r in results)
                        if (r < avgThroughput * 10)
                        {
                            inverseNormThroughputs += 1 / r;
                            inverseNormCount++;
                        }

                    avgThroughputNormalized = inverseNormCount / inverseNormThroughputs;
                }
                else avgThroughputNormalized = avgThroughput;

                recalcCount = results.Count;
            }
        }

        public void AddResultInternal(double result, long? position)
        {
            if (!double.IsInfinity(result))
            {
                results.Add(result);
                if (position != null) positions.Add(position.Value);
            }
        }

        public void AddResult(double result)
        {
            if (positions.Count > 0) throw new InvalidOperationException("You can't call this method once overload AddResult(double result, long position) has been called");

            AddResultInternal(result, null);
        }

        public void AddResult(double result, long position)
        {
            if (positions.Count != results.Count) throw new InvalidOperationException("You can't call this method once overload AddResult(double result) has been called");

            AddResultInternal(result, position);
        }

        public void AddTroughputMbs(long bytes, Stopwatch stopwatch)
        {
            double nanosecs = (double)stopwatch.ElapsedTicks * 1000000000 / Stopwatch.Frequency;

            AddResult(((double)bytes / 1024 / 1024) / (double)(nanosecs) * 1000 * 1000 * 1000);
        }

        public void AddTroughputMbs(long bytes, long position, Stopwatch stopwatch)
        {
            double secs = (double)stopwatch.ElapsedTicks / Stopwatch.Frequency;

            AddResult(((double)bytes / 1024 / 1024) / secs, position);
        }

        public double GetLatestResult()
        {
            return results[results.Count - 1];
        }

        public double GetLatest5AvgResult()
        {
            if (results.Count == 0) return 0;

            double inverseThroughputs = 0;

            for (var i = results.Count-1; i >= (results.Count - 5 > 0 ? results.Count - 5 : 0); i-- )
                inverseThroughputs += 1 / results[i];

            return results.Count - 5 > 0 ? 5 / inverseThroughputs : results.Count / inverseThroughputs;
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
