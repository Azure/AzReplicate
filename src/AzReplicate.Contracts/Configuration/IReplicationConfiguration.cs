namespace AzReplicate.Contracts.Configuration
{
    public class ReplicationConfigurationDefaults
    {
        public const string ReplicationQueueName = "replication";

        public const long ReplicationBlockSizeInBytes = 32 * 1024 * 1024;
    }

    public interface IReplicationConfiguration
    {
        long? ReplicationBlockSizeInBytes { get; set; }

        bool DisableGetSourceContentSizeInBytes { get; set; }

        bool DisableExistsDestinationWithCopySuccessAndSimilarSize { get; set; }
    }
}
