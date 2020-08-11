using AzReplicate.Contracts.Configuration;
using Microsoft.ApplicationInsights.WorkerService;
using System.Collections.Generic;

namespace AzReplicate
{
    public class ApplicationInsightsSettings : ApplicationInsightsServiceOptions, ITelemetryFilterConfiguration
    {
        public IEnumerable<string> FilteredDependencyTypes { get; set; }

        public ApplicationInsightsSettings()
        {
            EnableAdaptiveSampling = false;
        }
    }

    public class AppSettings : IQueueConfiguration, IQueueListeningConfiguration, ITableConfiguration, IReplicationConfiguration, ICorrelationConfiguration, ITelemetryConfiguration
    {
        public string CorrelationId { get; set; }

        public string QueueConnectionString { get; set; }

        public string QueueName { get; set; }

        public string TableConnectionString { get; set; }

        public int? QueueMessageReceiveBatchSize { get; set; }

        public int? QueueMessageInvisibilityTimeoutInSeconds { get; set; }

        public int? MaximumQueueRetrievalBackOffDurationInSeconds { get; set; }

        public int? MaximumNumberOfConcurrentMessageHandlers { get; set; }

        public int? MaximumNumberOfRetriesOnFailure { get; set; }

        public long? ReplicationBlockSizeInBytes { get; set; }

        public bool DisableGetSourceContentSizeInBytes { get; set; }

        public bool DisableExistsDestinationWithCopySuccessAndSimilarSize { get; set; }

        public string QueuedTelemetryName => "Replication Success";

        public string QueuedTelemetryMetricAggregatedCount => "ReplicatedSources";

        public string QueuedTelemetryMetricAggregatedDuration => "ReplicatedInSeconds";

        public string QueuedTelemetryMetricAggregatedSize => "ReplicatedBytes";
    }
}
