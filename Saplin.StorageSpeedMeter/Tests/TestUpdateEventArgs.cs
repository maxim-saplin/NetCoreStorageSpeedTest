namespace Saplin.StorageSpeedMeter
{
    public class TestUpdateEventArgs
    {
        /// <summary>
        /// Available aftre test is set to Completed status
        /// </summary>
        public TestResults Results { get; }
        /// <summary>
        /// Available in Running status
        /// </summary>
        public double? ProgressPercent { get; }
        /// <summary>
        /// Available in Running status
        /// </summary>
        public double? RecentResult { get; }
        /// <summary>
        /// Current test status
        /// </summary>
        public TestStatus Status;
        /// <summary>
        /// Available after status is set to Runnning
        /// </summary>
        public long? ElapsedMs { get; }

        public TestUpdateEventArgs(TestStatus status, double? progressPercent, double? recentResult, long? elapsedMs, TestResults results)
        {
            Results = results;
            ProgressPercent = progressPercent;
            RecentResult = recentResult;
            ElapsedMs = elapsedMs;
            Status = status;
        }
    }
}
