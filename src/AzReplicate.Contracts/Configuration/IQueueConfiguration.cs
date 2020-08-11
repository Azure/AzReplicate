namespace AzReplicate.Contracts.Configuration
{
    public interface IQueueConfiguration
    {
        string QueueConnectionString { get; }

        string QueueName { get;  }
    }
}
