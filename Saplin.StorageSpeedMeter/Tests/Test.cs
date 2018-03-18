using System;

namespace Saplin.StorageSpeedMeter
{
    public enum TestStatus { NotStarted, Started, Completed, Interrupted };

    public abstract class Test
    {
        public const string unitMbs = "MB/s";

        public abstract TestResults Execute();
        public abstract string Name { get; }
        public event EventHandler<TestUpdateEventArgs> StatusUpdate;

        protected bool breakCalled = false;

        private bool prerequsiteCleanup = false;

        // Read test might need file chace purged from RAM
        public bool PrerequsiteCleanup
        {
            get => prerequsiteCleanup; protected set => prerequsiteCleanup = value;
        }

        protected TestStatus status = TestStatus.NotStarted;
        
        protected void Update(string message, double? progressPercent = null)
        {
            StatusUpdate?.Invoke(this, new TestUpdateEventArgs(message, Status, progressPercent));
        }

        protected void FinalUpdate(TestResults results, long elapsedMs)
        {
            Update(string.Format("Avg(N): {7:0.00}, Avg: {1:0.00}{0}, Mean: {2:0.00}, Min: {3:0.00}{0}, Max: {4:0.00}{0}, Time: {5}m{6:00}s",
                results.Unit,
                results.AvgThoughput,
                results.Mean,
                results.Min,
                results.Max,
                elapsedMs / 1000 / 60,
                elapsedMs / 1000 % 60,
                results.AvgThoughputNormalized));
        }

        public void Break()
        {
            breakCalled = true;
            status = TestStatus.Interrupted;
        }

        public TestStatus Status
        {
            get
            {
                return status;
            }
            protected internal set
            {
                status = value;
            }
        }
    }
}