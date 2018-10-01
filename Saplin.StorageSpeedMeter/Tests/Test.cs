using System;
using System.Diagnostics;

namespace Saplin.StorageSpeedMeter
{
    public enum TestStatus { NotStarted, Started, InitMemBuffer, WarmigUp, Running, Completed, Interrupted, NotEnoughMemory, PurgingMemCache };

    public abstract class Test
    {
        public const string unitMbs = "MB/s";

        public abstract TestResults Execute();
        public abstract string DisplayName { get; }
        public string Name { get; set; }
        public event EventHandler<TestUpdateEventArgs> StatusUpdate;
        protected int blockSize;

        protected ICachePurger cachePurger;

        public int BlockSizeBytes { get { return blockSize; } }

        protected bool breakCalled = false;

        private TestStatus status = TestStatus.NotStarted;

        protected void Update(double? progressPercent = null, double? recentResult = null, long? elapsedMs = null, TestResults results = null)
        {
            StatusUpdate?.Invoke(this, new TestUpdateEventArgs(Status, progressPercent, recentResult, elapsedMs, results));
        }

        protected void FinalUpdate(TestResults results, long elapsedMs)
        {
            status = TestStatus.Completed; // do not assign property to avoid unnecessary event firing thet follows
            Update(100, null, elapsedMs, results);
        }

        protected void NotEnoughMemUpdate(TestResults results, long elapsedMs)
        {
            status = TestStatus.NotEnoughMemory; // do not assign property to avoid unnecessary event firing thet follows
            Update(100, null, elapsedMs, results);
        }

        public void Break()
        {
            breakCalled = true;
            if (status != TestStatus.NotStarted) Status = TestStatus.Interrupted;
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
                //TestResults interimResults = status == TestStatus.Completed ? res
                StatusUpdate?.Invoke(this, new TestUpdateEventArgs(status, null, null, ElapsedMs, null));
            }
        }

        private Stopwatch elapsedSw;

        protected Test()
        {
            Name = this.GetType().Name;
        }

        public long ElapsedMs
        {
            get
            {
                return elapsedSw == null ? 0 : elapsedSw.ElapsedMilliseconds;
            }
        }

        protected void RestartStopwatch()
        {
            elapsedSw = new Stopwatch();
            elapsedSw.Start();
        }

        protected long StopStopwatch()
        {
            elapsedSw.Stop();
            return elapsedSw.ElapsedMilliseconds;
        }
    }
}