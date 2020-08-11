namespace AzReplicate.Contracts.Configuration
{
    public class TelemetryConfigurationDefaults
    {
        public const string QueuedTelemetryName = "Success";

        public const string QueuedTelemetryMetricAggregatedCount = "AggregatedCount";

        public const string QueuedTelemetryMetricAggregatedDuration = "AggregatedDuration";

        public const string QueuedTelemetryMetricAggregatedSize = "AggregatedSize";
    }

    public interface ITelemetryConfiguration
    {
        string QueuedTelemetryName { get; }

        string QueuedTelemetryMetricAggregatedCount { get; }

        string QueuedTelemetryMetricAggregatedDuration { get; }

        string QueuedTelemetryMetricAggregatedSize { get; }
    }
}
