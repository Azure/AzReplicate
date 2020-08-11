namespace AzReplicate.Contracts.Telemetry
{
    public interface IQueueTelemetry
    {
        void Queue(QueuedTelemetry queuedTelemetry);

        void Flush();
    }
}
