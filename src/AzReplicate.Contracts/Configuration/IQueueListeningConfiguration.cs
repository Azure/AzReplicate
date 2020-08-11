namespace AzReplicate.Contracts.Configuration
{
    public class QueueListeningConfigurationDefaults
    {
        public const int QueueMessageReceiveBatchSize = 32;
        public const int QueueMessageInvisibilityTimeoutInSeconds = 5 * 60;

        public const int MaximumQueueRetrievalBackOffDurationInSeconds = 60;
        public const int MaximumNumberOfConcurrentMessageHandlers = 32;
        public const int MaximumNumberOfRetriesOnFailure = 0;
    }

    public interface IQueueListeningConfiguration
    {
        int? QueueMessageReceiveBatchSize { get; set; }

        int? QueueMessageInvisibilityTimeoutInSeconds { get; set; }

        int? MaximumQueueRetrievalBackOffDurationInSeconds { get; set; }

        int? MaximumNumberOfConcurrentMessageHandlers { get; set; }

        int? MaximumNumberOfRetriesOnFailure { get; set; }
    }
}