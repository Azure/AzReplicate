using AzReplicate.Contracts.Logging;
using AzReplicate.Contracts.Messaging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace AzReplicate.Core.Logging
{
    public class QueueLogger : ILogReplicationStatus
    {
        private const string SuccessQueueName = "completion";
        private const string FailedQueueName = "failures";

        private readonly ISendMessages _messageSender;

        public QueueLogger(ISendMessages messageSender)
        {
            _messageSender = messageSender;
        }

        public async Task SuccessAsync(Replicatable replicatable, TimeSpan replicatedIn = default, CancellationToken cancellationToken = default)
        {
            await _messageSender.SendAsync(replicatable, SuccessQueueName, cancellationToken);
        }

        public async Task FailureAsync(Replicatable replicatable, Exception e, CancellationToken cancellationToken = default)
        {
            await _messageSender.SendAsync(replicatable, FailedQueueName, cancellationToken);
        }

        public async Task UnprocessableAsync(string messageId, string popReceipt, string content, Exception e, CancellationToken cancellationToken = default)
        {
            await Task.CompletedTask;
        }
    }
}
