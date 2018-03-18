namespace Saplin.StorageSpeedMeter
{
    public class TestUpdateEventArgs
    {
        public string Message { get; }
        public double? ProgressPercent { get; }
        public TestStatus Status;

        public TestUpdateEventArgs(string message, TestStatus status, double? progressPercent)
        {
            Message = message;
            ProgressPercent = progressPercent;
            Status = status;
        }
    }
}
