using AzReplicate.Contracts.Configuration;
using AzReplicate.Contracts.Logging;
using AzReplicate.Contracts.Messaging;
using AzReplicate.Core.Exceptions;
using AzReplicate.Core.Extensions;
using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AzReplicate.Core.Messaging
{
    public class StorageQueueListener : IListenForMessages
    {
        private readonly int _maximumReceiveBatchSize;
        private readonly int _invisibilityTimeoutInSeconds;
        private readonly int _maximumBackOffDurationInSeconds;
        private readonly int _maximumHandleConcurrency;
        private readonly int _maximumNumberOfRetries;

        private CancellationToken _cancellationToken;
        private readonly SemaphoreSlim _concurrencyLimiter;

        private readonly QueueClient _queueClient;
        private readonly IHandleMessages _messageHandler;
        private readonly IEnumerable<ILogReplicationStatus> _replicationStatusLoggers;

        public StorageQueueListener(IQueueConfiguration queueConfiguration, IQueueListeningConfiguration queueListeningConfiguration, IHandleMessages messageHandler, IEnumerable<ILogReplicationStatus> replicationStatusLoggers) 
        {
            _messageHandler = messageHandler;
            _replicationStatusLoggers = replicationStatusLoggers;

            _maximumReceiveBatchSize = queueListeningConfiguration.QueueMessageReceiveBatchSize ?? QueueListeningConfigurationDefaults.QueueMessageReceiveBatchSize;
            _invisibilityTimeoutInSeconds = queueListeningConfiguration.QueueMessageInvisibilityTimeoutInSeconds ?? QueueListeningConfigurationDefaults.QueueMessageInvisibilityTimeoutInSeconds;
            _maximumBackOffDurationInSeconds = queueListeningConfiguration.MaximumQueueRetrievalBackOffDurationInSeconds ?? QueueListeningConfigurationDefaults.MaximumQueueRetrievalBackOffDurationInSeconds;
            _maximumHandleConcurrency = queueListeningConfiguration.MaximumNumberOfConcurrentMessageHandlers ?? QueueListeningConfigurationDefaults.MaximumNumberOfConcurrentMessageHandlers;
            _maximumNumberOfRetries = queueListeningConfiguration.MaximumNumberOfRetriesOnFailure ?? QueueListeningConfigurationDefaults.MaximumNumberOfRetriesOnFailure;

            _concurrencyLimiter = new SemaphoreSlim(_maximumHandleConcurrency, _maximumHandleConcurrency);

            _queueClient = new QueueClient(queueConfiguration.QueueConnectionString, queueConfiguration.QueueName ?? ReplicationConfigurationDefaults.ReplicationQueueName);
            _queueClient.CreateIfNotExists();
        }

        public async Task ReceiveAndHandleAsync(CancellationToken cancellationToken = default)
        {
            _cancellationToken = cancellationToken;

            while (!_cancellationToken.IsCancellationRequested)
            {
                var messages = await ReceiveMessagesWithLinearBackOffAsync();
                await HandleMessagesAsync(messages);
            }
        }

        private async Task HandleMessagesAsync(IEnumerable<QueueMessage> messages)
        {
            foreach (var message in messages)
            {
                await _concurrencyLimiter.WaitAsync(_cancellationToken);
                HandleMessageAsync(message).Ignore();
            }
        }

        private async Task HandleMessageAsync(QueueMessage message)
        {
            Replicatable replicatable = null;

            try
            {
                replicatable = MessageSerializer.Deserialize(message.MessageText);

                using (var executionTimer = new ExecutionTimer())
                {
                    await _messageHandler.HandleAsync(replicatable, _cancellationToken);

                    await _queueClient.DeleteMessageAsync(message.MessageId, message.PopReceipt, _cancellationToken);

                    await Task.WhenAll(_replicationStatusLoggers.Select(r => r.SuccessAsync(replicatable, executionTimer.CalculateElapsedAndStopMeasuring())));
                }
            }
            catch (UnsupportedMessageFormatException e)
            {
                await _queueClient.DeleteMessageAsync(message.MessageId, message.PopReceipt, _cancellationToken);

                await Task.WhenAll(_replicationStatusLoggers.Select(r => r.UnprocessableAsync(message.MessageId, message.PopReceipt, message.MessageText, e)));
            }
            catch (Exception e)
            {
                if (message.DequeueCount > _maximumNumberOfRetries)
                {
                    await _queueClient.DeleteMessageAsync(message.MessageId, message.PopReceipt, _cancellationToken);

                    await Task.WhenAll(_replicationStatusLoggers.Select(r => r.FailureAsync(replicatable, e)));
                }
            }
            finally
            {
                _concurrencyLimiter.Release();
            }
        }

        private async Task<IEnumerable<QueueMessage>> ReceiveMessagesWithLinearBackOffAsync()
        {
            var currentBackOffInSeconds = 0;
            var messages = new QueueMessage[0];

            while (!messages.Any())
            {
                await Task.Delay(TimeSpan.FromSeconds(Math.Min(currentBackOffInSeconds++, _maximumBackOffDurationInSeconds)), _cancellationToken);
                messages = (await _queueClient.ReceiveMessagesAsync(_maximumReceiveBatchSize, TimeSpan.FromSeconds(_invisibilityTimeoutInSeconds), _cancellationToken)).Value;
            }

            return messages;
        }
    }
}
