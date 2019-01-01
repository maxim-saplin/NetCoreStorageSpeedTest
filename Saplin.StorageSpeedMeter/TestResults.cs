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
        private double min, minN, max, maxN, mean, avgThroughputReal, avgThroughputNormalized;
        private const double normalizationTimeThreshold = 0.95;
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

            UseNormalizedAvg = test.IsNormalizedAvg;
        }

        public bool UseNormalizedAvg { get; set; }

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
        }

        public double Max
        {
            get
            {
                Recalculate();

                return max;
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
        }

        public double Mean
        {
            get
            {
                Recalculate();

                return mean;
            }
        }

        /// <summary>
        /// // Average throughput, either real or normalized
        /// </summary>
        public double AvgThroughput
        {
            get
            {
                Recalculate();

                return UseNormalizedAvg ? avgThroughputNormalized : avgThroughputReal;
            }
        }

        /// <summary>
        /// Throughput normalization assumes that some of the read measurements may come from RAM cache, rather than storage device, 
        /// and thus show speed of RAM, rather than storage device. To cancel out outliers test results are sorted ascending,
        /// total running time is calcuculated (as if in real test the speed was increasing gradually) and only those measures
        /// which account for 95% are used for avg calculation
        /// </summary>
        public double AvgThroughputNormalized
        {
            get
            {
                Recalculate();

                return avgThroughputNormalized;
            }
        }

        /// <summary>
        /// // Average throughput is not equal to mean of all thoughput measure and is calculated assuming that average equals to Total Traffic over Total Time
        /// </summary>
        public double AvgThroughputReal
        {
            get
            {
                Recalculate();

                return avgThroughputReal;
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
                var sorted = new double[results.Count];
                results.CopyTo(sorted);

                Array.Sort(sorted);

                min = sorted[0];
                max = sorted[sorted.Length - 1];
                minN = sorted[(int)(sorted.Length * .01)];
                maxN = sorted[(int)(sorted.Length * 0.99)];
                //mean = results.Average<double>(tr => tr);

                double inverseThroughputs = 0;// results.Select<double, double>(r => 1 / r).Sum();

                double totalTime = 0;
                mean = 0;

                foreach (var r in sorted)
                {
                    inverseThroughputs += 1 / r;
                    mean += r;
                    totalTime += BlockSizeBytes / (r * 1024 * 1024);
                }

                mean = mean / sorted.Length;

                avgThroughputReal = sorted.Length / inverseThroughputs; // AvgThougput = [TotalTrafic] / [TotalTime] ___OR___ [Number of thoughput measures] / SUM OF [1 / (Nth throughput measure)]

                double inverseNormThroughputs = 0;
                int inverseNormCount = 1;
                double normTime = 0;

                foreach (var r in sorted)
                {
                    inverseNormThroughputs += 1 / r;
                    normTime += BlockSizeBytes / (r * 1024 * 1024);
                    if (normTime > normalizationTimeThreshold * totalTime) break;
                    inverseNormCount++;
                }

                avgThroughputNormalized = inverseNormCount / inverseNormThroughputs;

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
