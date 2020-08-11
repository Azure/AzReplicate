using AzReplicate.Contracts.Logging;
using Microsoft.Azure.Cosmos.Table;

namespace AzReplicate.Core.Logging
{
    public class LogEntry : TableEntity, ILogEntry
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

        public LogEntry() { }

        public LogEntry(string partitionKey, string rowKey)
        {
            PartitionKey = partitionKey;
            RowKey = rowKey;
        }
    }
}
