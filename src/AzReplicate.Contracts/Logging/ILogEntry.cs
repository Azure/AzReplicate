namespace AzReplicate.Contracts.Logging
{
    public interface ILogEntry
    {
        public string CorrelationId { get; set; }

        public string RequestId { get; set; }

        public string Source { get; set; }

        public string Destination { get; set; }

        public string Content { get; set; }

        public double? ReplicatedInSeconds { get; set; }

        public long? ReplicatedBytes { get; set; }

        public string ExceptionReason { get; set; }

        public string Exception { get; set; }
    }
}
